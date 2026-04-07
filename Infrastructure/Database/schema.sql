-- Create schema for webhook platform
CREATE SCHEMA IF NOT EXISTS webhook_schema;

-- Create Payments table in webhook_schema
CREATE TABLE IF NOT EXISTS webhook_schema.payments (
    id UUID PRIMARY KEY,
    payment_id VARCHAR(255) NOT NULL,
    order_id VARCHAR(255) NOT NULL,
    amount NUMERIC(18, 2) NOT NULL,
    currency VARCHAR(10) NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    source VARCHAR(100) NOT NULL,
    received_at TIMESTAMP NOT NULL,
    processed_at TIMESTAMP,
    status VARCHAR(50) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create index for faster lookups
CREATE UNIQUE INDEX IF NOT EXISTS ux_webhook_payments_payment_id ON webhook_schema.payments(payment_id);
CREATE INDEX IF NOT EXISTS idx_webhook_payments_order_id ON webhook_schema.payments(order_id);
CREATE INDEX IF NOT EXISTS idx_webhook_payments_status ON webhook_schema.payments(status);
