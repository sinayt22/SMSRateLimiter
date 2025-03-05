#!/bin/bash

# This script generates load on the SMS Rate Limiter API by sending requests at varying rates
# It shows the metrics in real-time as they're collected by the API

API_URL="http://localhost:5139/api"

# Array of phone numbers to use
PHONE_NUMBERS=(
  "+14155551212"
  "+14155551213"
  "+14155551214"
  "+16505551212"
  "+16505551213"
  "+12125551212"
  "+12125551213"
  "+12125551214"
  "+12125551215"
)

# Counters for metrics display
declare -A total_requests
declare -A accepted_requests
declare -A rejected_requests

# Initialize counters
for phone in "${PHONE_NUMBERS[@]}"; do
  total_requests[$phone]=0
  accepted_requests[$phone]=0
  rejected_requests[$phone]=0
done

# Function to send a request for a specific phone number
send_request() {
  local phone_number=$1
  local response=$(curl -s -X POST "${API_URL}/RateLimiter/check" \
    -H "Content-Type: application/json" \
    -d "{\"phoneNumber\": \"$phone_number\"}")
  
  # Extract whether the request was allowed
  local allowed=$(echo $response | grep -o '"allowed":true' | wc -l)
  
  # Update counters
  total_requests[$phone_number]=$((total_requests[$phone_number] + 1))
  
  if [ $allowed -eq 1 ]; then
    accepted_requests[$phone_number]=$((accepted_requests[$phone_number] + 1))
    echo "Request for $phone_number was ALLOWED"
  else
    rejected_requests[$phone_number]=$((rejected_requests[$phone_number] + 1))
    echo "Request for $phone_number was REJECTED"
  fi
}

# Function to display metrics summary
display_metrics() {
  clear
  echo "=== SMS Rate Limiter Load Test ==="
  echo "Current time: $(date)"
  echo ""
  echo "Phone Number             | Requests | Accepted | Rejected | % Accepted"
  echo "--------------------------|----------|----------|----------|----------"
  
  for phone in "${PHONE_NUMBERS[@]}"; do
    total=${total_requests[$phone]}
    accepted=${accepted_requests[$phone]}
    rejected=${rejected_requests[$phone]}
    
    # Calculate percentage
    if [ $total -eq 0 ]; then
      percent="N/A"
    else
      percent=$(echo "scale=1; ($accepted * 100) / $total" | bc)
      percent="${percent}%"
    fi
    
    printf "%-26s | %-8d | %-8d | %-8d | %-10s\n" "$phone" "$total" "$accepted" "$rejected" "$percent"
  done
  
  echo ""
  echo "Press Ctrl+C to stop the load test."
}

# Main loop
echo "Starting load generation..."

# Set up trap to handle Ctrl+C gracefully
trap "echo 'Load generation stopped.'; exit 0" INT

# Use a counter to decide when to display metrics
counter=0

while true; do
  # Select a random phone number with weighted distribution
  # Some numbers get more traffic than others
  if [ $((RANDOM % 4)) -eq 0 ]; then
    # Use a "high traffic" phone number (first 3)
    rand_index=$((RANDOM % 3))
  else
    # Use any phone number
    rand_index=$((RANDOM % ${#PHONE_NUMBERS[@]}))
  fi
  
  phone=${PHONE_NUMBERS[$rand_index]}
  
  # Send the request
  send_request "$phone"
  
  # Increment counter and maybe display metrics
  counter=$((counter + 1))
  if [ $((counter % 10)) -eq 0 ]; then
    display_metrics
  fi
  
  # Random delay between 100ms and 1s
  # Creating a variable rate of requests
  if [ $((RANDOM % 5)) -eq 0 ]; then
    # Occasionally pause longer (up to 2s) to simulate traffic lulls
    delay=$((RANDOM % 1900 + 100))
  else
    # Normal delay
    delay=$((RANDOM % 900 + 100))
  fi
  
  sleep "0.$delay"
done