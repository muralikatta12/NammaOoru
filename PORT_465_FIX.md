# 🔄 Port 587 (SSL) Switch - Brevo Email Fix

## 🔍 Problem Identified

Your system was experiencing **consistent SMTP timeouts at 60 seconds** using **port 587 (StartTLS)**:

```
⚠️ [OTP] Attempt 1 failed: Operation timed out after 60000 milliseconds
   (at: MailKit.Net.Smtp.SmtpClient.PostConnectAsync — during TLS negotiation)
```

**What this means:**
- TCP connection to port 587 succeeds ✅
- But the SMTP protocol negotiation (waiting for server greeting + TLS setup) **hangs for 60 seconds**
- Indicates: Brevo's port 587 is either slow, overloaded, or there's network latency

---

## ✅ Solution Applied

### **Switch from Port 587 → Port 587**

**Port 587 (StartTLS):**
- Opens plain connection first
- Negotiates TLS upgrade
- More steps = more potential delays

**Port 587 (SSL/Implicit TLS):**
- SSL handshake happens immediately on connect
- Single connection, no upgrade steps
- Often faster, more direct

---

## 🔧 Changes Made

### File 1: `appsettings.json`
```json
"EmailSettings": {
  "SmtpServer": "smtp-relay.brevo.com",
  "SmtpPort": 587,  // ← Changed from 587
  "SenderEmail": "9b93cb001@smtp-brevo.com",
  "SenderPassword": "xsmtpsib-..."
}
```

### File 2: `Services/EmailService.cs` — 3 methods updated

Each method now:
1. **Uses port 587 as default** (if not configured)
2. **Auto-detects secure socket option:**
   ```csharp
   var secureSocketOptions = smtpPort == 587 ? 
       SecureSocketOptions.SslOnConnect :  // Port 587 = SSL from start
       SecureSocketOptions.StartTls;       // Port 587 = TLS upgrade
   ```
3. **Timeout increased to 60 seconds** (all methods)

---

## 📊 Expected Behavior Now

| Aspect | Before (Port 587) | After (Port 587) |
|--------|-------------------|------------------|
| Connection type | StartTLS (gradual) | SSL (direct) |
| Handshake steps | 2-3 steps | 1 step |
| Typical speed | Slow/Timeout-prone | Fast/Direct |
| Brevo Dashboard | ❌ 0 emails sent | ✅ Emails visible |

---

## 🚀 How to Test

### 1. Start the app:
```powershell
cd "c:\Users\2417663\OneDrive - Cognizant\Desktop\NammaOoru\NammaOoru"
dotnet run
```

### 2. Send OTP request:
```powershell
$body = @{ email = "kattamurali611@gmail.com" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:5077/api/auth/send-otp" `
  -Method Post -Body $body -ContentType "application/json"
```

### 3. Watch for these logs:

**Success (port 587 working):**
```
📧 [OTP] SMTP: smtp-relay.brevo.com:587 | From: 9b93cb001@smtp-brevo.com
✅ [OTP] Connected to SMTP
✅ [OTP] Authenticated as 9b93cb001@smtp-brevo.com
✅ [OTP] Email sent to SMTP queue
```

**If still timing out (port 587 also slow):**
```
⚠️ [OTP] Attempt 1 failed: Operation timed out after 60000 milliseconds
⏳ [OTP] Waiting 2000ms before retry 2...
📧 [OTP] Attempt 2/3 | Sending to kattamurali611@gmail.com
```

Then retries will continue with exponential backoff.

---

## 📋 Why Port 587 Usually Works Better

1. **Less protocol overhead** — SSL from the start, no "upgrade" dance
2. **Faster initial handshake** — single TLS negotiation
3. **More predictable** — industry standard for implicit SSL
4. **Brevo optimized** — their infrastructure likely better optimized for 587
5. **Reduces intermediate timeouts** — fewer steps = fewer timeout points

---

## ⚙️ If You Need Port 587 Again

You can override in `appsettings.json`:
```json
"EmailSettings": {
  "SmtpPort": 587  // Back to port 587
}
```

The code will **auto-detect** and use `SecureSocketOptions.StartTls` automatically.

---

## 📝 Summary of All Changes

| File | Change |
|------|--------|
| `appsettings.json` | `SmtpPort: 587` → `587` |
| `EmailService.cs` - `SendOtpEmailAsync` | Default port 587 + SSL detection + 60s timeout |
| `EmailService.cs` - `SendWelcomeEmailAsync` | Default port 587 + SSL detection + 60s timeout |
| `EmailService.cs` - `SendNotificationEmailAsync` | Default port 587 + SSL detection + 60s timeout |

---

## ✨ Next Steps

1. **Run the app** with the new port 587 settings
2. **Send test OTP** and watch console
3. **Check your email inbox** for the OTP (within 5-30 seconds)
4. **Verify Brevo dashboard** — go to Statistics, confirm "Sent" count increased
5. **If successful** — all done! Email sending now works reliably
6. **If still failing** — Port 587 might also be throttled; we can troubleshoot further

---

**Status:** ✅ **Build succeeded. Ready to test with port 587.**

Try it now and let me know if emails arrive! 🚀

