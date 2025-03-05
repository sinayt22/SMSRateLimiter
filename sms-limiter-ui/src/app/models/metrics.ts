export interface BucketStatus {
  phoneNumber: string;
  phoneNumberCurrentTokens: number;
  phoneNumberMaxTokens: number;
  phoneNumberRefillRate: number;
  globalCurrentTokens: number;
  globalMaxTokens: number;
  globalRefillRate: number;
  phoneNumberLastUsed: Date;
  globalLastUsed: Date;
}

export interface MessageMetrics {
  timestamp: Date;
  phoneNumber: string;
  requestCount: number;
  acceptedCount: number;
  rejectedCount: number;
}

export interface PhoneNumberMetrics {
  phoneNumber: string;
  totalRequests: number;
  acceptedRequests: number;
  rejectedRequests: number;
  percentAccepted: number;
  requestsPerSecond: number;
  currentTokens: number;
  maxTokens: number;
}

export interface GlobalMetrics {
  totalRequests: number;
  acceptedRequests: number;
  rejectedRequests: number;
  percentAccepted: number;
  requestsPerSecond: number;
  currentTokens: number;
  maxTokens: number;
}

export interface MetricTimeSeries {
  timestamp: Date;
  requests: number;
  accepted: number;
  rejected: number;
}