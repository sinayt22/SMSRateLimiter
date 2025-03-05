# SMS Rate Limiter

A robust API and dashboard for rate limiting SMS messages with real-time monitoring.

## Project Overview

SMS Rate Limiter is a comprehensive solution for controlling SMS message traffic through configurable rate limits. It consists of:

1. **API**: A .NET Core Web API that implements token bucket rate limiting
2. **Dashboard**: An Angular-based UI for monitoring rate limiting metrics in real-time
3. **Load Testing Tools**: Scripts for generating test traffic to demonstrate rate limiting behavior

## Core Features

- **Token Bucket Algorithm**: Efficient rate limiting with configurable token refill rates
- **Dual Rate Limiting**: Limits applied at both per-phone-number and global levels
- **Real-time Monitoring**: Dashboard shows current token levels and traffic patterns
- **Metrics Collection**: Records and displays accept/reject rates and traffic volumes
- **Configurable Settings**: Adjustable rate limits, refill rates and timeout thresholds

## System Architecture

The system is built with a clean architecture pattern:

- **Core Layer**: Contains the token bucket implementation and interfaces
- **API Layer**: Provides HTTP endpoints for rate limiting and metrics
- **UI Layer**: Angular dashboard for monitoring and visualization

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Node.js 18+ and npm
- Angular CLI 19+

### Running the API

```bash
# Build and run the API
./run.sh
```

The API will be available at http://localhost:5139

### Running the UI Dashboard

```bash
# Navigate to the UI directory
cd sms-limiter-ui

# Install dependencies
npm install

# Run the development server
ng serve
```

The dashboard will be available at http://localhost:4200

### Testing With Load Generator

To simulate traffic and see the rate limiting in action:

```bash
# Make the script executable
chmod +x generate-load.sh

# Run the load generator
./generate-load.sh
```

## Configuration

Rate limiting parameters can be adjusted in `appsettings.json`:

```json
"RateLimiter": {
  "PhoneNumberMaxRateLimit": 20,
  "GlobalRateMaxRateLimit": 100,
  "PhoneNumberRefillRatePerSecond": 2,
  "GlobalRefillRatePerSecond": 10,
  "BucketCleanupIntervalMilliSec": 60000,
  "InactiveBucketTimeoutMilliSec": 300000
}
```

## API Endpoints

### Rate Limiting

- `POST /api/RateLimiter/check`: Check if a message can be sent (rate limited)
- `GET /api/RateLimiter/status/{phoneNumber}`: Get current token bucket status

### Metrics

- `GET /api/Metrics/messages`: Get rate limiting metrics with optional filtering
- `GET /api/Metrics/phones`: Get a list of all phone numbers being tracked
- `GET /api/Metrics/summary`: Get aggregated metrics summary
- `POST /api/Metrics/messages`: Record a new metric (used internally)
- `DELETE /api/Metrics/clear`: Clear all stored metrics

## Implementation Details

### Token Bucket Algorithm

The system uses the token bucket algorithm, where:

1. Each phone number has its own bucket with configured capacity
2. A global bucket applies limits across all phone numbers
3. Tokens refill at configurable rates per second
4. Each SMS request consumes one token when allowed
5. Requests are rejected when insufficient tokens are available

### Memory Management

- Inactive token buckets are automatically cleaned up to prevent memory leaks
- Configurable timeouts determine when buckets are removed from memory

## Extension Points

The system is designed for extensibility:

- `ITokenBucketProvider`: Can be replaced with distributed implementations (Redis, etc.)
- `IRateLimiterService`: Can be extended for more complex limiting strategies
- Metrics: Can be enhanced to store data in external databases