# 📧 Email Delivery Troubleshooting Guide

## ✅ Current Status
- **SMTP Connection:** ✅ Working (smtp-relay.brevo.com:587)
- **Authentication:** ✅ Working (9b93cb001@smtp-brevo.com)
- **Email Sending:** ✅ Working (No errors in logs)
- **Receipt Issue:** ⚠️ Emails not appearing in Inbox

---

## 🔍 Why You're Not Receiving OTP Emails

### **Most Common Reason: Emails Going to SPAM FOLDER** 📬➡️📋

Gmail filters these emails as spam because:
1. Brevo's relay email (`9b93cb001@smtp-brevo.com`) is not a branded domain
2. Missing SPF/DKIM/DMARC DNS records
3. New sender has low reputation

### **Solution: Check These Locations**

#### **1. Gmail Spam Folder** (First priority!)
```
Go to: Gmail → Left Sidebar → "More" → "All Mail"
Then filter by "From: nammaooru@"
```

**If you find emails there:**
- ✅ Click the email
- ✅ Click "Report not spam" / "Mark as Not Spam"
- ✅ Check "Always allow mail from [sender]"
- ✅ Add to Contacts

#### **2. Gmail Promotions Tab**
Gmail automatically sorts transactional emails here sometimes.

**If emails are there:**
- ✅ Drag to Inbox
- ✅ Or disable Promotions tab in Gmail Settings

#### **3. Other Folders to Check**
- Spam
- Trash
- Updates
- Social

---

## 🧪 How to Test OTP Email Sending

### **Option 1: Using Swagger UI (Easy)**
1. Go to: `http://localhost:5077`
2. Find `/auth/send-otp` endpoint
3. Click "Try it out"
4. Enter your email: `muralikatta15@gmail.com`
5. Click "Execute"
6. **Watch the console logs** in your terminal for ✅ or ❌ indicators

### **Option 2: Using curl (Terminal)**
```bash
curl -X POST http://localhost:5077/auth/send-otp \
  -H "Content-Type: application/json" \
  -d '{"email":"muralikatta15@gmail.com"}'
```

### **Option 3: Using Postman**
- POST to: `http://localhost:5077/auth/send-otp`
- Body (JSON):
```json
{
  "email": "muralikatta15@gmail.com"
}
```

---

## 📊 Console Log Indicators

When you send OTP, look for these logs:

### ✅ **Success (Emails will be sent)**
```
📧 [OTP] Sending to muralikatta15@gmail.com
✅ [OTP] Connected to SMTP
✅ [OTP] Authenticated
✅ [OTP] Sent successfully!
✅ [OTP] Email delivered to muralikatta15@gmail.com
```

### ❌ **Error (Emails won't be sent)**
```
❌ [OTP] ERROR: [error message here]
```

---

## 🛠️ If Emails Are Still Not Appearing

### **Step 1: Verify Email Address**
- Make sure you're using the **EXACT email** you registered with
- Check for typos in email address

### **Step 2: Check Brevo Account**
- Login to: https://app.brevo.com/dashboard
- Go to **Logs** → Check if emails show as "Sent"
- Check **Bounce/Spam** status

### **Step 3: Check Gmail Filters**
- Gmail Settings → Filters and Blocked Addresses
- Delete any filter that might block the sender

### **Step 4: Whitelist Sender**
In Gmail, create a filter:
1. Go to Settings → Filters and Blocked Addresses
2. Create new filter
3. From: `9b93cb001@smtp-brevo.com`
4. Apply label or Never send to spam

### **Step 5: Try Different Email**
- Use different email provider (Outlook, Yahoo, etc.)
- Sometimes Gmail filters are stricter

---

## 📝 Alternative: Use Your Own Domain

For production, set up branded email with your domain:

1. **Get custom domain** (e.g., noreply@nammaooru.com)
2. **Add SPF record** in DNS:
   ```
   v=spf1 include:sendingdomain.brevo.com ~all
   ```
3. **Add DKIM record** from Brevo
4. **Update appsettings.json**:
   ```json
   "EmailSettings": {
     "SenderEmail": "noreply@nammaooru.com"
   }
   ```

This will dramatically improve email delivery!

---

## 🚀 Temporary Workaround

**For development/testing:**
- Use `noreply@example.com` as sender (fake domain)
- Check console logs to verify OTP was generated
- Manually verify the OTP code from logs instead of checking email

**In logs, you'll see:**
```
OTP sent to user@gmail.com
OTP code: 123456  <-- Use this for testing
```

---

## 📞 Still Having Issues?

### **Check These:**
1. ✅ Is the app running? (Should show "Now listening on: http://localhost:5077")
2. ✅ Are console logs showing SUCCESS or ERROR?
3. ✅ Did you check Spam/Promotions folders?
4. ✅ Is there a typo in the email address?
5. ✅ Are SMTP credentials correct in `appsettings.json`?

### **Debug Steps:**
1. Stop the app (Ctrl+C)
2. Rebuild: `dotnet build`
3. Run with logs: `dotnet run`
4. Send OTP via Swagger
5. Copy the console output and share it

---

## 🎯 Next Steps

**Short term:**
1. Check Spam/Promotions folder ✅
2. Mark emails as "Not Spam"
3. Add sender to contacts

**Medium term:**
1. Set up branded domain
2. Add SPF/DKIM records
3. Improve sender reputation

**Long term:**
1. Implement email retry queue (Phase 2-D)
2. Add bounce handling
3. Monitor delivery metrics

---

## 📚 Useful Resources

- [Gmail Spam Filters](https://support.google.com/mail/answer/6587)
- [Brevo Email Deliverability](https://www.brevo.com/help/article/what-should-i-do-if-my-emails-arent-being-delivered/)
- [SPF/DKIM/DMARC Explanation](https://www.dmarcian.com/)

