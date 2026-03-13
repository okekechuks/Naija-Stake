# JWT Secrets Management Setup

This guide explains how to configure JWT secrets securely for the Naija-Stake API.

## Security Best Practices

**NEVER commit JWT secrets to version control.** The `SecretKey` in `appsettings.json` is intentionally empty. You must configure it using one of the methods below.

## Development Setup

### Option 1: User Secrets (Recommended for Local Development)

User secrets are stored outside your project directory and are automatically loaded in development.

1. **Initialize user secrets** (if not already done):
   ```bash
   cd Backend/src/NaijaStake.API
   dotnet user-secrets init
   ```

2. **Set the JWT secret key**:
   ```bash
   dotnet user-secrets set "JwtSettings:SecretKey" "your-super-secret-jwt-key-minimum-32-characters-long-for-security"
   ```

   Or set via environment variable:
   ```bash
   dotnet user-secrets set "JWT_SECRET_KEY" "your-super-secret-jwt-key-minimum-32-characters-long-for-security"
   ```

3. **Verify it's set**:
   ```bash
   dotnet user-secrets list
   ```

### Option 2: Environment Variables

Set the `JWT_SECRET_KEY` environment variable:

**Windows (PowerShell):**
```powershell
$env:JWT_SECRET_KEY = "your-super-secret-jwt-key-minimum-32-characters-long-for-security"
```

**Windows (Command Prompt):**
```cmd
set JWT_SECRET_KEY=your-super-secret-jwt-key-minimum-32-characters-long-for-security
```

**Linux/Mac:**
```bash
export JWT_SECRET_KEY="your-super-secret-jwt-key-minimum-32-characters-long-for-security"
```

**Note:** Environment variables take precedence over user secrets and appsettings.json.

## Production Setup

### Option 1: Environment Variables (Recommended)

Set the `JWT_SECRET_KEY` environment variable in your hosting environment:

- **Azure App Service**: Configuration → Application Settings → Add `JWT_SECRET_KEY`
- **AWS Elastic Beanstalk**: Environment Properties → Add `JWT_SECRET_KEY`
- **Docker**: Use `-e JWT_SECRET_KEY=...` or `.env` file
- **Kubernetes**: Use Secrets and ConfigMaps

### Option 2: Azure Key Vault

For Azure deployments, use Azure Key Vault:

```csharp
// In Program.cs (add after builder creation)
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{vaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

Then reference the secret:
```json
{
  "JwtSettings": {
    "SecretKey": "@Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/JwtSecretKey/)"
  }
}
```

### Option 3: AWS Secrets Manager

For AWS deployments:

```csharp
// Add AWS Secrets Manager configuration
builder.Configuration.AddSecretsManager();
```

## Secret Key Requirements

- **Minimum length**: 32 characters
- **Recommended**: 64+ characters for better security
- **Format**: Any string (base64, hex, random text, etc.)
- **Generation**: Use a cryptographically secure random generator

### Generate a Secure Secret Key

**Using PowerShell:**
```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

**Using OpenSSL:**
```bash
openssl rand -base64 32
```

**Using .NET:**
```csharp
var bytes = new byte[32];
using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
{
    rng.GetBytes(bytes);
}
var secret = Convert.ToBase64String(bytes);
```

## Configuration Priority

The application loads secrets in this order (later values override earlier ones):

1. `appsettings.json` (lowest priority)
2. `appsettings.{Environment}.json`
3. User Secrets (Development only)
4. Environment Variables (highest priority)

## Verification

After setting up secrets, verify the application starts correctly:

```bash
cd Backend/src/NaijaStake.API
dotnet run
```

If the secret is not configured or is too short, you'll see an error:
```
InvalidOperationException: JWT SecretKey is not configured...
```

## Rotating Secrets

**Important:** When rotating JWT secrets:

1. Generate a new secret key
2. Update the secret in your environment/user secrets
3. **All existing tokens will become invalid** - users will need to log in again
4. Consider a gradual rollout if you need to maintain session continuity

## Troubleshooting

### Error: "JWT SecretKey is not configured"

- Check that you've set the secret using one of the methods above
- Verify the environment variable name is `JWT_SECRET_KEY` (all caps)
- In development, ensure user secrets are initialized

### Error: "JWT SecretKey must be at least 32 characters"

- Your secret key is too short
- Generate a new key with at least 32 characters

### Tokens not working after secret change

- This is expected - old tokens were signed with the old secret
- Users need to log in again to get new tokens

## Security Checklist

- [ ] Secret key is at least 32 characters long
- [ ] Secret key is not committed to version control
- [ ] Secret key is stored securely (environment variables, key vault, etc.)
- [ ] Secret key is different for each environment (dev/staging/prod)
- [ ] Secret key rotation plan is documented
- [ ] Access to secrets is restricted (only authorized personnel)
