#!/bin/bash

# Webhook Testing Script
# This script generates test webhooks with valid signatures for the platform

SECRET="development-secret-key"
API_URL="http://localhost:5000"  # Update if running on different port

# Function to generate HMAC-SHA256 signature
generate_signature() {
    local payload="$1"
    local secret="$2"
    echo -n "$payload" | openssl dgst -sha256 -hmac "$secret" -hex | sed 's/^.* //' | sed 's/^/sha256=/'
}

# Test 1: Valid payment completed event
echo "Test 1: Valid Payment Completed Event"
PAYLOAD='{"eventType":"payment.completed","source":"Stripe","timestamp":"2024-04-13T10:30:00Z","data":{"paymentId":"pay_123456","orderId":"order_789","amount":99.99,"currency":"USD"}}'
SIGNATURE=$(generate_signature "$PAYLOAD" "$SECRET")
echo "Signature: $SIGNATURE"

curl -X POST "$API_URL/webhook/incoming" \
  -H "Content-Type: application/json" \
  -H "X-Signature: $SIGNATURE" \
  -d "$PAYLOAD" \
  -v

echo -e "\n\n---\n"

# Test 2: Invalid signature
echo "Test 2: Invalid Signature (should fail with 401)"
INVALID_SIGNATURE="sha256=0000000000000000000000000000000000000000000000000000000000000000"

curl -X POST "$API_URL/webhook/incoming" \
  -H "Content-Type: application/json" \
  -H "X-Signature: $INVALID_SIGNATURE" \
  -d "$PAYLOAD" \
  -v

echo -e "\n\n---\n"

# Test 3: Invalid JSON (should fail with 400)
echo "Test 3: Invalid JSON (should fail with 400)"
INVALID_JSON='{"invalid json'
SIGNATURE=$(generate_signature "$INVALID_JSON" "$SECRET")

curl -X POST "$API_URL/webhook/incoming" \
  -H "Content-Type: application/json" \
  -H "X-Signature: $SIGNATURE" \
  -d "$INVALID_JSON" \
  -v

echo -e "\n\n---\n"

# Test 4: Missing required fields
echo "Test 4: Missing Required Fields (should fail with 400)"
PAYLOAD_BAD='{"eventType":"payment.completed","source":"Stripe","timestamp":"2024-04-13T10:30:00Z","data":{"paymentId":"","orderId":"order_789","amount":-10,"currency":"USD"}}'
SIGNATURE=$(generate_signature "$PAYLOAD_BAD" "$SECRET")

curl -X POST "$API_URL/webhook/incoming" \
  -H "Content-Type: application/json" \
  -H "X-Signature: $SIGNATURE" \
  -d "$PAYLOAD_BAD" \
  -v

echo -e "\n\nTest complete!"
