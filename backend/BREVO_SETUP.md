# 🚀 Brevo SMTP Setup - Complete Guide

## 🔴 Current Problem
- ✅ SMTP connection logs show "success"
- ❌ Brevo dashboard shows 0 emails sent
- ❌ No emails received by users

**Root Cause:** SMTP credentials are invalid or sender email not verified in Brevo

---

## ✅ Solution: Get Correct Brevo Credentials

### **Step 1: Login to Brevo Dashboard**
1. Go to: https://app.brevo.com/dashboard
2. Login with your account

### **Step 2: Get Your Sender Email**
1. Go to **Senders & API**
2. Click **Senders** (left menu)
3. Look for your **verified sender email** (e.g., `noreply@domain.com` or your email)
4. **Copy this email** - this is your `SenderEmail`

### **Step 3: Get SMTP Credentials**
1. In Brevo, go to **Senders & API** → **SMTP & API**
2. Click **SMTP** tab
3. You'll see:
   - **SMTP Server:** `smtp-relay.brevo.com`
   - **Port:** `587`
   - **Login:** Your Brevo username or email
   - **Password:** Your SMTP API key (looks like: `xsmtpsib-...`)

### **Step 4: Update appsettings.json**

Replace the `EmailSettings` section with your actual credentials:

```json
"EmailSettings": {
  "SmtpServer": "smtp-relay.brevo.com",
  "SmtpPort": 587,
  "SenderEmail": "[YOUR_VERIFIED_SENDER_EMAIL]",
  "SenderPassword": "[YOUR_SMTP_API_KEY]"
}
```

**Example:**
```json
"EmailSettings": {
  "SmtpServer": "smtp-relay.brevo.com",
  "SmtpPort": 587,
  "SenderEmail": "muralikatta15@gmail.com",
  "SenderPassword": "xsmtpsib-106294097d0287e17b2ba77eba7cd182ba13e9110821ce120fb43272b4baff33-3NlI2oUZPDlmrN3W"
}
```

**⚠️ IMPORTANT:**
- `SenderEmail` must be a **verified sender** in your Brevo account
- `SenderPassword` must be your actual **SMTP API key**, not your Brevo password

---

## 🔍 How to Verify Sender Email in Brevo

If you don't see a verified sender:

1. In Brevo, go to **Senders & API** → **Senders**
2. Click **"Add a sender"**
3. Enter your email (e.g., `noreply@nammaooru.com` or personal email)
4. **Verify the email** (Brevo sends confirmation link)
5. Once verified ✅, use it in `appsettings.json`

---

## 🧪 Test After Update

1. **Save** `appsettings.json`
2. **Restart** the app: `dotnet run`
3. **Send OTP** via Swagger: `POST /auth/send-otp`
4. **Check:**
   - Console logs for ✅ or ❌
   - Brevo dashboard → **Statistics** → Check "Sent" count increases
   - Your email inbox

---

## 📊 How to Check Brevo Stats

1. Go to Brevo dashboard
2. Click **Statistics** (left menu)
3. Look for:
   - **Sent:** Should show > 0
   - **Delivered:** Should show > 0
   - **Bounced:** Should be 0 or minimal

If **Sent = 0**, your credentials are wrong.

---

## 🆘 Troubleshooting

### **Issue: "Authentication failed"**
- ❌ Wrong SMTP API key
- ✅ Solution: Copy EXACT key from Brevo SMTP tab
- ✅ Make sure no extra spaces

### **Issue: "Sender email not recognized"**
- ❌ Sender email not verified in Brevo
- ✅ Solution: Add and verify sender in Brevo first

### **Issue: "Connection timeout"**
- ❌ Wrong server or port
- ✅ Solution: Use `smtp-relay.brevo.com:587` (not `:25` or `:587`)

### **Issue: Emails still not received**
1. Check Brevo dashboard - does it show "Sent"?
2. If yes → emails going to spam (see EMAIL_TROUBLESHOOTING.md)
3. If no → credentials issue (follow this guide)

---

## 🔐 Security Note

**Never commit `appsettings.json` with real credentials to GitHub!**

Better approach for production:
- Use **Environment Variables**
- Use **Azure Key Vault**
- Use **appsettings.Production.json** (gitignored)

For now (development), it's OK to have credentials in `appsettings.json` locally.

---

## 📝 Quick Checklist

Before testing, verify:
- [ ] Brevo account created
- [ ] Sender email verified in Brevo
- [ ] SMTP credentials copied correctly
- [ ] `appsettings.json` updated with correct values
- [ ] App restarted after config change
- [ ] Console shows "Authenticating with [correct-email]"

---

## 🚀 Next Steps

1. Go to Brevo dashboard
2. Copy your verified sender email
3. Copy your SMTP API key
4. Update `appsettings.json`
5. Restart app
6. Test OTP sending
7. Verify in Brevo statistics

