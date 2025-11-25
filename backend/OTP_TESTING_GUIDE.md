# 🧪 OTP & Email Testing Guide

## What Was Fixed

### Problem
- ✅ API endpoint `/api/auth/send-otp` was hanging (kept loading)
- ✅ SMTP connection to Brevo was timing out after 15 seconds
- ✅ OTP was stored in DB but email was never sent due to timeout

### Root Cause
The SMTP connection to `smtp-relay.brevo.com:587` was experiencing **slow TLS/SSL handshake** or **server latency**, causing a timeout during protocol negotiation (not during file transfer).

### Solutions Applied
1. **Increased timeout** from 15 seconds → 60 seconds
2. **Added retry logic** with exponential backoff:
   - Attempt 1: Tries to connect
   - Attempt 2: Waits 2s, retries
   - Attempt 3: Waits 4s, retries
3. **Background task** (fire-and-forget) so API returns immediately
4. **Better logging** to track each attempt

---

## 🚀 How to Test

### Step 1: Start the App
```powershell
cd "c:\Users\2417663\OneDrive - Cognizant\Desktop\NammaOoru\NammaOoru"
dotnet run
```

Wait for:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5077
```

### Step 2: Send OTP Request

**Option A: Using cURL (PowerShell)**
```powershell
$body = @{
    email = "your-email@gmail.com"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5077/api/auth/send-otp" `
  -Method Post `
  -Body $body `
  -ContentType "application/json"
```

**Option B: Using Swagger/Postman**
1. Open: http://localhost:5077/swagger
2. Find `/api/auth/send-otp` endpoint
3. Click "Try it out"
4. Enter: `{ "email": "your-email@gmail.com" }`
5. Click "Execute"

**Option C: Using .http file**
If you have the `Moodly.API.http` file in VS Code:
```
POST http://localhost:5077/api/auth/send-otp
Content-Type: application/json

{
  "email": "your-email@gmail.com"
}
```

### Step 3: Watch Console Output

**Expected success logs:**
```
info: NammaOoru.Services.AuthService[0]
      📧 [Background] Starting OTP email send to your-email@gmail.com

info: NammaOoru.Services.EmailService[0]
      📧 [OTP] Attempt 1/3 | Sending to your-email@gmail.com

info: NammaOoru.Services.EmailService[0]
      📧 [OTP] SMTP: smtp-relay.brevo.com:587 | From: 9b93cb001@smtp-brevo.com

info: NammaOoru.Services.EmailService[0]
      ✅ [OTP] Connected to SMTP

info: NammaOoru.Services.EmailService[0]
      ✅ [OTP] Authenticated as 9b93cb001@smtp-brevo.com

info: NammaOoru.Services.EmailService[0]
      ✅ [OTP] Email sent to SMTP queue

info: NammaOoru.Services.EmailService[0]
      ✅ [OTP] Disconnected from SMTP

info: NammaOoru.Services.EmailService[0]
      ✅ [OTP] Email delivered to your-email@gmail.com
```

**Expected if it retries (slow connection):**
```
warn: NammaOoru.Services.EmailService[0]
      ⚠️ [OTP] Attempt 1 failed: Operation timed out after 60000 milliseconds

info: NammaOoru.Services.EmailService[0]
      ⏳ [OTP] Waiting 2000ms before retry 2...

info: NammaOoru.Services.EmailService[0]
      📧 [OTP] Attempt 2/3 | Sending to your-email@gmail.com

info: NammaOoru.Services.EmailService[0]
      ✅ [OTP] Connected to SMTP

...SUCCESS...
```

### Step 4: Verify Email Received

1. Check your email inbox (including spam/promotions)
2. Look for email from "noreply-nammaooru@..." or "9b93cb001@smtp-brevo.com"
3. Subject: "🔐 Your OTP Verification Code - NammaOoru"

### Step 5: Verify in Brevo Dashboard (Optional)

1. Go to https://app.brevo.com
2. Click **Statistics** → **Sent**
3. Confirm the count increased

---

## ⚠️ If Email Still Doesn't Arrive

### Scenario 1: All retries fail (still timing out)
**Log:** `❌ [OTP] All 3 attempts failed to send OTP to ...`

**Action:** Check network/firewall:
```powershell
# Verify Brevo is still reachable
Test-NetConnection -ComputerName smtp-relay.brevo.com -Port 587
```

**Result should be:**
```
TcpTestSucceeded : True
```

If `False`, contact your IT/network team about port 587 firewall rules.

---

### Scenario 2: Email says "AUTHENTICATION FAILED"
**Log:** `❌ [OTP] AUTHENTICATION FAILED!`

**Action:** Verify Brevo credentials in `appsettings.json`:
1. Open: https://app.brevo.com
2. Go to **Senders & API** → **SMTP & API** tab
3. Copy the exact password (looks like `xsmtpsib-...`)
4. Paste into `appsettings.json` → `EmailSettings:SenderPassword`
5. Restart the app

---

### Scenario 3: Email is sent but not received
**Log:** `✅ [OTP] Email delivered to ...`

**Action:** Email went to spam or wasn't verified:
1. Check spam/promotions folder
2. Verify sender in Brevo:
   - Go to **Senders & API** → **Senders**
   - Confirm `9b93cb001@smtp-brevo.com` is verified ✅
   - If not, click "Add sender" and verify it

---

## 📊 Expected Behavior After Fix

| Scenario | Before | After |
|----------|--------|-------|
| API response | Hangs for 15-60s | Returns **immediately** (email sent in background) |
| SMTP timeout | Fails after 15s | Retries up to 3 times over ~6 seconds |
| Slow connection | No emails sent | Succeeds on retry 2-3 |
| Database | OTP saved ✅ | OTP saved ✅ |
| Email delivery | ❌ Times out | ✅ Sent (if credentials OK) |

---

## 🔍 Debugging Tips

### View all logs in real-time
The app is configured to log at `Information` level. All `📧 [OTP]`, `✅`, `⚠️`, `❌` messages are visible.

### Enable verbose SQL logging (if needed)
In `appsettings.json`, change:
```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.EntityFrameworkCore": "Information"  // Change from Warning to Information
  }
}
```

### Test with different email addresses
Some corporate email systems are strict. Try:
- Gmail
- Hotmail
- Your personal email
- A test email at your organization

---

## 📝 Files Changed

- **`Services/EmailService.cs`**
  - Added retry loop (3 attempts max)
  - Increased timeout to 60s
  - Added exponential backoff (2s, 4s delays)
  - Better attempt tracking logs

- **`Services/AuthService.cs`**
  - Email send already runs in background (no API blocking)

---

## ✅ Next Steps

1. **Run the app** and send an OTP
2. **Watch console logs** for success indicators
3. **Check email inbox**
4. **Verify Brevo stats** (optional)

If still failing after retries, use the debugging scenarios above to pinpoint the issue.

---

## 🆘 Need More Help?

Check the console logs for:
- Exact error message (copy the Exception part)
- Which attempt failed
- Whether it's SMTP Connect, Authenticate, or Send phase
- Whether it's actually trying to retry

Then share the logs, and we can debug further!

---

**Last Updated:** November 20, 2025
