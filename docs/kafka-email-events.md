# Kafka email events

The User service publishes `EmailNotificationRequested` events to `fundoo.email.v1`. The Messaging service consumes the topic with consumer group `fundoo-messaging-email` and sends the email through its existing SMTP service.

## Start Apache Kafka locally

```powershell
docker compose -f docker-compose.kafka.yml up -d
```

The local broker is available at `localhost:9092`.

## Enable Kafka

Set `Kafka:UseKafka` to `true` in the User API configuration and `Kafka:Enabled` to `true` in the Messaging API configuration. The defaults remain disabled so existing HTTP communication continues to work when Kafka is not running.

The User service has `Kafka:FallbackToHttp` enabled by default. If publishing fails, it uses the retained `HttpMessagingClient`. This can be disabled when Kafka-only delivery is required.

The HTTP client implementation remains in `Fundoo.User.Infrastructure/Http/HttpMessagingClient.cs`; it is not removed and is selected by the feature flag or fallback behavior.

## Delivery behavior

- User publishes with `acks=all` and idempotence enabled.
- Messaging commits an offset only after SMTP delivery succeeds.
- Malformed events are logged and committed so they do not block the partition.
- SMTP failures remain uncommitted and can be retried after consumer restart.
- Do not log event bodies, reset tokens, or OTP values.
