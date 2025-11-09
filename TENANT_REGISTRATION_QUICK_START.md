# 🚀 Quick Start - Tenant Registration API

## API Endpoint

```
POST https://localhost:5001/api/v1/tenants/register
Content-Type: application/json
```

## Request Example

```json
{
  "schoolName": "Trường Mầm Non Hoa Hồng",
  "subdomain": "truong-hoa-hong",
  "contactEmail": "contact@hoahong.edu.vn",
  "contactPhone": "0901234567",
  "address": "123 Nguyễn Văn A, Quận 1, TP.HCM",
  "adminFullName": "Nguyễn Văn Admin",
  "adminPhoneNumber": "0912345678",
  "adminEmail": "admin@hoahong.edu.vn",
  "adminPassword": "Admin@12345"
}
```

## Response Example

```json
{
  "success": true,
  "data": {
    "tenantId": "7f8e9d6c-5b4a-3c2d-1e0f-9a8b7c6d5e4f",
    "tenantName": "Trường Mầm Non Hoa Hồng",
    "subdomain": "truong-hoa-hong",
    "accessUrl": "https://truong-hoa-hong.emis.com",
    "adminUserId": "3a2b1c0d-9e8f-7d6c-5b4a-3c2d1e0f9a8b",
    "adminPhoneNumber": "0912345678",
    "adminFullName": "Nguyễn Văn Admin",
    "subscriptionPlan": "Trial",
    "subscriptionExpiresAt": "2025-12-10T10:30:00Z",
    "maxUsers": 50,
    "createdAt": "2025-11-10T10:30:00Z"
  }
}
```

## Test with curl

```bash
curl -X POST "https://localhost:5001/api/v1/tenants/register" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "schoolName": "Trường Mầm Non ABC",
    "subdomain": "truong-abc",
    "contactEmail": "contact@abc.edu.vn",
    "contactPhone": "0901111111",
    "adminFullName": "Admin ABC",
    "adminPhoneNumber": "0902222222",
    "adminPassword": "Admin@12345"
  }'
```

## Then Login

```bash
curl -X POST "https://localhost:5001/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "phoneNumber": "0902222222",
    "password": "Admin@12345"
  }'
```

## Validation Rules

### School Info
- **schoolName**: 3-255 ký tự
- **subdomain**: 3-50 ký tự, lowercase/số/dấu gạch ngang, UNIQUE
- **contactEmail**: Email hợp lệ
- **contactPhone**: 10 số, bắt đầu 0

### Admin Info  
- **adminFullName**: 2-255 ký tự
- **adminPhoneNumber**: 10 số, bắt đầu 0, UNIQUE globally
- **adminPassword**: Min 8 ký tự, phải có:
  - ✓ Uppercase letter
  - ✓ Lowercase letter
  - ✓ Number
  - ✓ Special character (@, !, #, $, etc.)

## Common Errors

### Subdomain Already Exists
```json
{
  "success": false,
  "error": {
    "code": "SUBDOMAIN_EXISTS",
    "message": "Subdomain 'truong-abc' is already taken..."
  }
}
```

### Phone Already Registered
```json
{
  "success": false,
  "error": {
    "code": "ADMIN_PHONE_EXISTS",
    "message": "Phone number '0902222222' is already registered..."
  }
}
```

### Weak Password
```json
{
  "success": false,
  "errors": {
    "AdminPassword": [
      "Admin password must contain at least one uppercase letter",
      "Admin password must contain at least one special character"
    ]
  }
}
```

## Reserved Subdomains

❌ Không được dùng: `admin`, `api`, `www`, `mail`, `ftp`, `localhost`, `app`, `portal`

## Subscription Plans

| Plan | Max Users | Trial Days |
|------|-----------|------------|
| Trial | 50 | 30 |
| Basic | 100 | - |
| Standard | 500 | - |
| Professional | 2000 | - |
| Enterprise | Unlimited | - |

Mặc định tenant mới sẽ là **Trial plan** (30 ngày miễn phí).
