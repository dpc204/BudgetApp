# 401 Error Diagnostic Checklist

**Run through these steps IN ORDER and report EXACTLY what you see:**

## 1. Check if you can sign in at all
- Navigate to: `https://localhost:59611`
- **Are you redirected to Microsoft sign-in?** YES / NO
  ## No
- **Can you complete sign-in?** YES / NO
  ## It didn't look like I needed to sign in.  The nav menu showed my userID and that I was a member of Admin role.
- **After sign-in, what happens?** (describe)
  ## part of the startup process calls the Api.  Got a 401 error
## 2. Check the Output Window IMMEDIATELY after the 401 error
In Visual Studio, View ? Output ? Show output from: **Debug**
  ## pasted at end of this document

Look for these EXACT log messages and copy/paste them here:

```
Search for: "Attempting to acquire token for scope"
Found: (paste the line)

## 19:26:33:347	Budget.Web.Services.ForwardAuthCookiesHandler: Information: Attempting to acquire token for scope: api://36ca674b-1c79-49ad-98fb-b90f13d72887/access_as_user

Search for: "Added Bearer token" OR "Failed to acquire access token"  
Found: (paste the line)
  ## Neither message was found

Search for: "User consent required" OR "MSAL UI interaction required"
Found: (paste the line)
  ## Budget.Web.Services.ForwardAuthCookiesHandler: Error: ? User consent required for https://localhost:7063/api/useroptions/hCJExs7Pay8CDXuBi1xe_ZvIpwmuDs-Ai-8UlZbZzxw. MsalError: user_null

Search for: "user_null" OR "interaction_required"
Found: (paste the line)
```

## 3. Check your browser Network tab
- Press F12 in browser
- Go to Network tab
- Make the request that causes 401
- Find the failing request
- Click on it ? Headers tab

**Request Headers:**
- Is there an `Authorization: Bearer ...` header? YES / NO
- If YES, copy first 50 characters of the token value

**Response:**
- Status code: (should be 401)
- Response body: (paste it)

## 4. Check your user secrets
Run this command:
```powershell
Get-Content "$env:APPDATA\Microsoft\UserSecrets\37834f08-c42f-4f1e-9a80-3911e57d81ac\secrets.json"
```

**Is AzureAd:ClientId set?** YES / NO  
**Does it match in both Budget.Web and Budget.Api secrets?** YES / NO

## 5. Check SQL SessionCache table
Run this query:
```sql
SELECT COUNT(*) FROM dbo.SessionCache
SELECT TOP 1 Id, ExpiresAtTime FROM dbo.SessionCache ORDER BY ExpiresAtTime DESC
```

**Row count:**
**Most recent expiration time:**

---

## Report ALL findings above, then I'll tell you EXACTLY what's wrong.
