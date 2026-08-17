#!/usr/bin/env bash
#
# test_nfr.sh — Automated validation of NFR-01..NFR-07 for the MediDB Backend
#
# Purpose: produce reproducible, timestamped evidence for the "Non-functional
# requirements" table of the academic report. Every check prints PASS / FAIL /
# MANUAL and appends a line to a Markdown results log so it can be pasted
# straight into the report's testing/validation appendix.
#
# Usage:
#   cp test_nfr.env.example test_nfr.env   # fill in the values
#   ./test_nfr.sh                          # reads test_nfr.env if present
#
# Config can also be supplied purely via environment variables, e.g.:
#   BASE_URL=https://medidb.voxvoltera.com TEST_EMAIL=doc@clinic.dk \
#   TEST_PASSWORD=... TEST_CPR=010190-1234 ./test_nfr.sh
#
# Requires: curl, jq, openssl (all standard on any Linux/macOS box).

set -u
set -o pipefail

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Explicit environment variables (e.g. `BASE_URL=... ./test_nfr.sh`) must win
# over test_nfr.env, not get silently clobbered by it. Snapshot anything the
# caller already set, source the file, then restore the caller's values.
_ENV_VARS="BASE_URL TEST_EMAIL TEST_PASSWORD TEST_CPR JWT_SECRET JWT_ISSUER JWT_AUDIENCE EXPECTED_SESSION_MINUTES PERF_BUDGET_SECONDS PERF_SAMPLES RESULTS_FILE AUDIT_DB_URL AUDIT_TABLE"
for _v in $_ENV_VARS; do
  if [ -n "${!_v:-}" ]; then
    eval "_PRESET_${_v}=\${${_v}}"
  fi
done

[ -f "$SCRIPT_DIR/test_nfr.env" ] && source "$SCRIPT_DIR/test_nfr.env"

for _v in $_ENV_VARS; do
  _preset_name="_PRESET_${_v}"
  if [ -n "${!_preset_name:-}" ]; then
    eval "${_v}=\${${_preset_name}}"
  fi
done
unset _v _preset_name _ENV_VARS

BASE_URL="${BASE_URL:-http://localhost:8080}"

# A clinic-staff account with the "Doctor" position and NO MFA enabled
# (MFA-enabled accounts stop at the 202 challenge step and cannot be
# driven through this black-box script without a live TOTP secret).
TEST_EMAIL="${TEST_EMAIL:-}"
TEST_PASSWORD="${TEST_PASSWORD:-}"

# An existing patient CPR in the DDMMYY-XXXX format expected by
# ptdataFetchingRequest (see Controllers/DoctorPatientInterfaceController.cs).
TEST_CPR="${TEST_CPR:-}"

# Only needed to forge an already-expired JWT for NFR-05. Must match the
# server's Jwt:Key / Jwt:Issuer / Jwt:Audience exactly (appsettings.json /
# environment). Leave blank to skip NFR-05's forged-token check (it will be
# reported as MANUAL instead of PASS/FAIL).
JWT_SECRET="${JWT_SECRET:-}"
JWT_ISSUER="${JWT_ISSUER:-}"
JWT_AUDIENCE="${JWT_AUDIENCE:-}"

# Expected inactivity/session lifetime per NFR-01, in minutes.
EXPECTED_SESSION_MINUTES="${EXPECTED_SESSION_MINUTES:-60}"

# Perf budget per NFR-03, in seconds, and how many samples to average.
PERF_BUDGET_SECONDS="${PERF_BUDGET_SECONDS:-2.0}"
PERF_SAMPLES="${PERF_SAMPLES:-5}"

RESULTS_FILE="${RESULTS_FILE:-$SCRIPT_DIR/nfr_results.md}"

# ---------------------------------------------------------------------------
# Plumbing
# ---------------------------------------------------------------------------

PASS=0; FAIL=0; MANUAL=0
RUN_TS="$(date -u +'%Y-%m-%dT%H:%M:%SZ')"

C_RESET="\033[0m"; C_GREEN="\033[32m"; C_RED="\033[31m"; C_YELLOW="\033[33m"; C_BOLD="\033[1m"

echo "# NFR validation run — $RUN_TS" > "$RESULTS_FILE"
echo "" >> "$RESULTS_FILE"
echo "Target: \`$BASE_URL\`" >> "$RESULTS_FILE"
echo "" >> "$RESULTS_FILE"
echo "| ID | Result | Detail |" >> "$RESULTS_FILE"
echo "|----|--------|--------|" >> "$RESULTS_FILE"

log_result() {
  local id="$1" status="$2" detail="$3"
  case "$status" in
    PASS)   PASS=$((PASS+1));   printf "${C_GREEN}[PASS]${C_RESET}   %-7s %s\n" "$id" "$detail" ;;
    FAIL)   FAIL=$((FAIL+1));   printf "${C_RED}[FAIL]${C_RESET}   %-7s %s\n" "$id" "$detail" ;;
    MANUAL) MANUAL=$((MANUAL+1)); printf "${C_YELLOW}[MANUAL]${C_RESET} %-7s %s\n" "$id" "$detail" ;;
  esac
  # escape pipes for the markdown table
  local esc="${detail//|/\\|}"
  echo "| $id | $status | $esc |" >> "$RESULTS_FILE"
}

require() {
  for bin in "$@"; do
    command -v "$bin" >/dev/null 2>&1 || { echo "Missing required tool: $bin" >&2; exit 1; }
  done
}
require curl jq openssl

b64url() {
  # stdin -> base64url, no padding
  openssl base64 -A | tr '+/' '-_' | tr -d '='
}

section() { printf "\n${C_BOLD}== %s ==${C_RESET}\n" "$1"; }

# ---------------------------------------------------------------------------
# Auth bootstrap: log in once, reuse the access token for every test that
# needs one. Login itself doubles as a smoke test of the auth pipeline.
# ---------------------------------------------------------------------------

ACCESS_TOKEN=""
LOGIN_RESPONSE=""
LOGIN_HEADERS=""
LOGIN_TS_UNIX=""

if [ -n "$TEST_EMAIL" ] && [ -n "$TEST_PASSWORD" ]; then
  section "Bootstrap: logging in as $TEST_EMAIL"
  LOGIN_TMP="$(mktemp)"
  LOGIN_HDR_TMP="$(mktemp)"
  LOGIN_TS_UNIX="$(date +%s)"
  HTTP_CODE=$(curl -s -o "$LOGIN_TMP" -D "$LOGIN_HDR_TMP" -w '%{http_code}' \
    -X POST "$BASE_URL/api/um/ac/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$TEST_EMAIL\",\"password\":\"$TEST_PASSWORD\"}")
  LOGIN_RESPONSE="$(cat "$LOGIN_TMP")"
  LOGIN_HEADERS="$(cat "$LOGIN_HDR_TMP")"
  rm -f "$LOGIN_TMP" "$LOGIN_HDR_TMP"

  if [ "$HTTP_CODE" = "200" ]; then
    ACCESS_TOKEN="$(echo "$LOGIN_RESPONSE" | jq -r '.accessToken // empty')"
    if [ -z "$ACCESS_TOKEN" ]; then
      echo "Login returned 200 but no accessToken — is MFA enabled on this account? Falling back to unauthenticated checks only." >&2
    else
      echo "Login OK, access token acquired."
    fi
  elif [ "$HTTP_CODE" = "202" ]; then
    echo "Login returned 202 (MFA challenge). Use an MFA-disabled test account so this script can obtain a bearer token." >&2
  else
    echo "Login failed (HTTP $HTTP_CODE). Authenticated checks will be skipped/marked MANUAL." >&2
  fi
else
  echo "TEST_EMAIL/TEST_PASSWORD not set — authenticated checks will be skipped/marked MANUAL." >&2
fi

AUTH_HEADER=()
[ -n "$ACCESS_TOKEN" ] && AUTH_HEADER=(-H "Authorization: Bearer $ACCESS_TOKEN")

# A small representative set of protected endpoints, spanning every
# [Authorize]/policy in the codebase, used by NFR-05 and NFR-06.
# format: METHOD|PATH|BODY(optional)
PROTECTED_ENDPOINTS=(
  "POST|/api/um/fetch|{\"email\":\"$TEST_EMAIL\"}"
  "GET|/api/um/mfa/status|"
  "POST|/api/dpm/usrfet/journal|{\"CPR_pt\":\"$TEST_CPR\"}"
  "POST|/api/pm/reg|{}"
  "POST|/api/sudo/lam/fetch|{}"
)

# ---------------------------------------------------------------------------
# NFR-01: session must expire after 60 minutes of inactivity, then redirect
# to login instead of showing an error.
#
# The backend is a stateless bearer-token API (no server session cookie for
# authenticated users — see AddSession() usage, which is only used by the
# Passkey/WebAuthn ceremony, not by ac/login). "Session length" is therefore
# enforced as the JWT access-token lifetime (Jwt:ExpiryMinutes), and the
# "redirect to login" behaviour is implemented client-side once a 401 comes
# back. This script can only verify the backend half automatically.
# ---------------------------------------------------------------------------
section "NFR-01: 60-minute session/token expiry"

if [ -n "$ACCESS_TOKEN" ]; then
  PAYLOAD_B64="$(echo "$ACCESS_TOKEN" | cut -d. -f2)"
  # restore padding for base64 -d
  MOD=$(( ${#PAYLOAD_B64} % 4 ))
  [ "$MOD" -eq 2 ] && PAYLOAD_B64="${PAYLOAD_B64}=="
  [ "$MOD" -eq 3 ] && PAYLOAD_B64="${PAYLOAD_B64}="
  CLAIMS="$(echo "$PAYLOAD_B64" | tr '_-' '/+' | openssl base64 -d -A 2>/dev/null)"
  EXP="$(echo "$CLAIMS" | jq -r '.exp // empty')"
  IAT="$(echo "$CLAIMS" | jq -r '.iat // empty')"

  # The token has no "iat" claim (TokenService.GenerateToken never sets
  # NotBefore/IssuedAt), so lifetime can't be computed as exp-iat. Fall back
  # to exp minus the wall-clock time this script captured right before the
  # login request — a +/-1 min tolerance absorbs request latency and any
  # client/server clock skew.
  if [ -z "$IAT" ]; then
    echo "Note: JWT has no 'iat' claim (TokenService.GenerateToken doesn't set NotBefore); measuring lifetime against client-observed login time instead." >&2
  fi

  if [ -n "$EXP" ] && [ -n "$LOGIN_TS_UNIX" ]; then
    LIFETIME_MIN=$(( (EXP - LOGIN_TS_UNIX) / 60 ))
    DIFF=$(( LIFETIME_MIN - EXPECTED_SESSION_MINUTES ))
    [ "$DIFF" -lt 0 ] && DIFF=$(( -DIFF ))
    if [ "$DIFF" -le 1 ]; then
      log_result "NFR-01" "PASS" "Access token lifetime is ~${LIFETIME_MIN} min (exp vs. observed login time), matches the ${EXPECTED_SESSION_MINUTES}-min requirement within tolerance. NOTE: this is an absolute token TTL from issuance, not a sliding inactivity timer, and the redirect-to-login behaviour lives in the frontend — verify both manually (see report)."
    else
      log_result "NFR-01" "FAIL" "Access token lifetime is ~${LIFETIME_MIN} min, expected ${EXPECTED_SESSION_MINUTES} min. Check Jwt:ExpiryMinutes in server config."
    fi
  else
    log_result "NFR-01" "MANUAL" "Could not decode the exp claim from the access token."
  fi
else
  log_result "NFR-01" "MANUAL" "No access token available (set TEST_EMAIL/TEST_PASSWORD to an MFA-disabled account). Frontend redirect-on-expiry also requires manual/browser verification regardless."
fi

# ---------------------------------------------------------------------------
# NFR-02: every view/edit of a patient journal must be written to an
# append-only audit log (timestamp, user ID, patient ID), retained >= 12mo.
#
# No audit-log table, service, or query endpoint exists in the current
# codebase (Services/, Models/, Controllers/ contain no AuditLog type; the
# only request logging is app-wide method+path+status to stdout via the
# middleware in Program.cs, which is neither per-record nor queryable nor
# guaranteed append-only/retained). This cannot be validated black-box over
# HTTP because there is nothing to query. Reported as a gap, not skipped.
# ---------------------------------------------------------------------------
section "NFR-02: append-only audit log on journal access"

AUDIT_ENDPOINT_FOUND=0
for candidate in "/api/audit" "/api/audit/log" "/api/dpm/audit" "/api/um/audit"; do
  CODE=$(curl -s -o /dev/null -w '%{http_code}' "${AUTH_HEADER[@]}" "$BASE_URL$candidate")
  if [ -n "$CODE" ] && [ "$CODE" != "404" ] && [ "$CODE" != "000" ]; then
    AUDIT_ENDPOINT_FOUND=1
    break
  fi
done

if [ "$AUDIT_ENDPOINT_FOUND" -eq 1 ]; then
  log_result "NFR-02" "MANUAL" "An endpoint under a guessed audit path responded (not 404) — inspect it by hand and confirm it records timestamp/user ID/patient ID with 12-month retention."
else
  log_result "NFR-02" "FAIL" "No audit-log table/service/endpoint found in the codebase (grep for AuditLog/audit_log across Models/Services/Controllers turns up nothing); only ad-hoc console request logging exists. Requirement is not implemented. Optional: point AUDIT_DB_URL at Postgres in test_nfr.env to have this script query for an audit table directly."
fi

# Optional stronger check: if the tester exports AUDIT_DB_URL (a psql
# connection string) and AUDIT_TABLE, verify a row is actually inserted
# when the journal endpoint is hit.
if [ -n "${AUDIT_DB_URL:-}" ] && [ -n "${AUDIT_TABLE:-}" ] && command -v psql >/dev/null 2>&1; then
  BEFORE=$(psql "$AUDIT_DB_URL" -Atc "SELECT count(*) FROM $AUDIT_TABLE;" 2>/dev/null)
  curl -s -o /dev/null "${AUTH_HEADER[@]}" -X POST "$BASE_URL/api/dpm/usrfet/journal" \
    -H "Content-Type: application/json" -d "{\"CPR_pt\":\"$TEST_CPR\"}"
  AFTER=$(psql "$AUDIT_DB_URL" -Atc "SELECT count(*) FROM $AUDIT_TABLE;" 2>/dev/null)
  if [ -n "$BEFORE" ] && [ -n "$AFTER" ] && [ "$AFTER" -gt "$BEFORE" ]; then
    log_result "NFR-02-DB" "PASS" "Row count in $AUDIT_TABLE increased ($BEFORE -> $AFTER) after a journal read."
  else
    log_result "NFR-02-DB" "FAIL" "Row count in $AUDIT_TABLE did not increase after a journal read ($BEFORE -> $AFTER)."
  fi
fi

# ---------------------------------------------------------------------------
# NFR-03: on a >10 Mbps connection, a journal must be readable within 2s.
#
# This script measures server + network response time for the journal fetch
# endpoint from wherever it runs. It is a proxy for the requirement, not a
# substitute for a controlled 10 Mbps throttled test — note that in the
# report.
# ---------------------------------------------------------------------------
section "NFR-03: journal read latency < ${PERF_BUDGET_SECONDS}s"

if [ -n "$ACCESS_TOKEN" ] && [ -n "$TEST_CPR" ]; then
  TOTAL="0"
  MAX="0"
  ALL_OK=1
  for i in $(seq 1 "$PERF_SAMPLES"); do
    TIME=$(curl -s -o /dev/null "${AUTH_HEADER[@]}" -w '%{time_total}' \
      -X POST "$BASE_URL/api/dpm/usrfet/journal" \
      -H "Content-Type: application/json" \
      -d "{\"CPR_pt\":\"$TEST_CPR\"}")
    TOTAL=$(awk -v a="$TOTAL" -v b="$TIME" 'BEGIN{printf "%.3f", a+b}')
    OVER=$(awk -v t="$TIME" -v budget="$PERF_BUDGET_SECONDS" 'BEGIN{print (t>budget)?1:0}')
    [ "$OVER" -eq 1 ] && ALL_OK=0
    MAXCMP=$(awk -v m="$MAX" -v t="$TIME" 'BEGIN{print (t>m)?1:0}')
    [ "$MAXCMP" -eq 1 ] && MAX="$TIME"
  done
  AVG=$(awk -v total="$TOTAL" -v n="$PERF_SAMPLES" 'BEGIN{printf "%.3f", total/n}')
  if [ "$ALL_OK" -eq 1 ]; then
    log_result "NFR-03" "PASS" "avg=${AVG}s max=${MAX}s over ${PERF_SAMPLES} samples, all under ${PERF_BUDGET_SECONDS}s. (Server-side/local-network measurement; not a substitute for a throttled 10 Mbps client test.)"
  else
    log_result "NFR-03" "FAIL" "avg=${AVG}s max=${MAX}s over ${PERF_SAMPLES} samples — at least one sample exceeded ${PERF_BUDGET_SECONDS}s."
  fi
else
  log_result "NFR-03" "MANUAL" "Need ACCESS_TOKEN and TEST_CPR to exercise the journal endpoint."
fi

# ---------------------------------------------------------------------------
# NFR-04: session cookie must be Secure, HttpOnly, SameSite=Strict.
#
# Confirmed architecture (2026-08-17): the backend returns the access/refresh
# tokens in the JSON body (Services/auth.cs::IssueTokens) and the FRONTEND
# writes them into a cookie client-side via JavaScript (document.cookie).
# This is a structural, not incidental, problem for this requirement:
# HttpOnly is a flag the browser only honours on a cookie set via a
# server Set-Cookie response header — script-set cookies (document.cookie)
# can never be HttpOnly, by design (that's the entire point of the flag: to
# hide the cookie from JS, including the JS that would otherwise need to
# set it). So as long as the frontend is the one cookie-ing the token,
# NFR-04 cannot be satisfied no matter how the cookie is configured
# client-side — this is verified below as a deterministic FAIL, not
# treated as "no cookie found".
#
# Remediation is an architecture change, not a config tweak: the backend
# would need to set the access/refresh token itself via Set-Cookie on the
# login/refresh response (with HttpOnly/Secure/SameSite=Strict), and the
# frontend would stop touching the token directly and rely on the browser
# attaching the cookie automatically (credentials: 'include' + CORS
# AllowCredentials, which is already enabled in Program.cs).
# ---------------------------------------------------------------------------
section "NFR-04: auth cookie flags (Secure/HttpOnly/SameSite=Strict)"

SET_COOKIE_LINES=$(echo "$LOGIN_HEADERS" | grep -i '^set-cookie:' || true)
if [ -z "$SET_COOKIE_LINES" ]; then
  log_result "NFR-04" "FAIL" "Confirmed: the backend sets no Set-Cookie header on login (accessToken/refreshToken are returned in the JSON body, Services/auth.cs::IssueTokens) and the frontend cookies the token itself via document.cookie. A script-set cookie can never carry HttpOnly (only a server Set-Cookie header can), so NFR-04 is structurally unsatisfiable under the current split — this is not a missing test, it is a real gap. Fix requires moving cookie-setting server-side (Set-Cookie on /api/um/ac/login and /api/um/ac/refresh) and having the frontend stop handling the token directly."
else
  BAD=0
  while IFS= read -r line; do
    echo "$line" | grep -qi 'secure' || BAD=1
    echo "$line" | grep -qi 'httponly' || BAD=1
    echo "$line" | grep -qi 'samesite=strict' || BAD=1
  done <<< "$SET_COOKIE_LINES"
  if [ "$BAD" -eq 0 ]; then
    log_result "NFR-04" "PASS" "All Set-Cookie headers include Secure, HttpOnly and SameSite=Strict."
  else
    log_result "NFR-04" "FAIL" "At least one Set-Cookie header is missing Secure/HttpOnly/SameSite=Strict: $SET_COOKIE_LINES"
  fi
fi

# ---------------------------------------------------------------------------
# NFR-05: an expired JWT must be rejected on every protected endpoint, not
# just at page load.
# ---------------------------------------------------------------------------
section "NFR-05: expired JWT rejected on every protected endpoint"

if [ -n "$JWT_SECRET" ] && [ -n "$ACCESS_TOKEN" ]; then
  SUB="$(echo "$CLAIMS" | jq -r '.sub // "00000000-0000-0000-0000-000000000000"')"
  EMAIL_CLAIM="$(echo "$CLAIMS" | jq -r '.email // "test@example.com"')"
  POSITION_CLAIM="$(echo "$CLAIMS" | jq -r '.position // "Doctor"')"
  CLINIC_CLAIM="$(echo "$CLAIMS" | jq -r '.clinic // ""')"
  ISS="${JWT_ISSUER:-$(echo "$CLAIMS" | jq -r '.iss // ""')}"
  AUD="${JWT_AUDIENCE:-$(echo "$CLAIMS" | jq -r '.aud // ""')}"

  NOW=$(date +%s)
  HEADER_JSON='{"alg":"HS256","typ":"JWT"}'
  H_B64=$(printf '%s' "$HEADER_JSON" | b64url)

  sign_forged_token() {
    # $1 = exp (unix seconds), $2 = jti suffix
    local exp="$1" jti="$2" payload p_b64 sig
    payload=$(jq -nc \
      --arg sub "$SUB" --arg email "$EMAIL_CLAIM" --arg position "$POSITION_CLAIM" \
      --arg clinic "$CLINIC_CLAIM" --arg iss "$ISS" --arg aud "$AUD" --arg jti "$jti" \
      --argjson exp "$exp" \
      '{sub:$sub, email:$email, position:$position, clinic:$clinic, jti:$jti, iss:$iss, aud:$aud, exp:$exp}')
    p_b64=$(printf '%s' "$payload" | b64url)
    sig=$(printf '%s' "${H_B64}.${p_b64}" | openssl dgst -sha256 -hmac "$JWT_SECRET" -binary | b64url)
    echo "${H_B64}.${p_b64}.${sig}"
  }

  # Sanity check FIRST: forge a token with a VALID (future) exp using the
  # supplied JWT_SECRET and confirm the server actually accepts it. If it
  # doesn't, JWT_SECRET doesn't match the server's real signing key, and any
  # 401 we get from the "expired" token below would prove nothing about
  # expiry enforcement specifically (it'd just be a bad-signature rejection).
  SANITY_JWT=$(sign_forged_token $((NOW + 3600)) "nfr-test-sanity")
  SANITY_CODE=$(curl -s -o /dev/null -w '%{http_code}' -X GET "$BASE_URL/api/um/mfa/status" \
    -H "Authorization: Bearer $SANITY_JWT")

  if [ "$SANITY_CODE" != "200" ]; then
    log_result "NFR-05" "MANUAL" "JWT_SECRET does not appear to match the server's signing key: a freshly forged, non-expired token signed with it was rejected (HTTP $SANITY_CODE on GET /api/um/mfa/status, expected 200). Re-check Jwt:Key in the server's config and update JWT_SECRET in test_nfr.env, then re-run. (A 401 on an already-expired token would not be meaningful evidence with the wrong key.)"
  else
    EXPIRED_JWT=$(sign_forged_token $((NOW - 3600)) "nfr-test-expired")

    ALL_REJECTED=1
    DETAILS=""
    for entry in "${PROTECTED_ENDPOINTS[@]}"; do
      IFS='|' read -r METHOD EP_PATH BODY <<< "$entry"
      CODE=$(curl -s -o /dev/null -w '%{http_code}' -X "$METHOD" "$BASE_URL$EP_PATH" \
        -H "Authorization: Bearer $EXPIRED_JWT" \
        -H "Content-Type: application/json" \
        ${BODY:+-d "$BODY"})
      if [ "$CODE" != "401" ]; then
        ALL_REJECTED=0
        DETAILS="${DETAILS}${METHOD} ${EP_PATH} -> ${CODE}; "
      fi
    done

    if [ "$ALL_REJECTED" -eq 1 ]; then
      log_result "NFR-05" "PASS" "JWT_SECRET verified correct (sanity token accepted). Forged expired JWT (exp=1h in the past, valid signature) was then rejected with 401 on all ${#PROTECTED_ENDPOINTS[@]} sampled protected endpoints."
    else
      log_result "NFR-05" "FAIL" "JWT_SECRET verified correct (sanity token accepted), but the expired JWT was NOT rejected with 401 on: $DETAILS"
    fi
  fi
else
  log_result "NFR-05" "MANUAL" "Set JWT_SECRET (and optionally JWT_ISSUER/JWT_AUDIENCE) in test_nfr.env to let this script forge an expired-but-validly-signed token. Without it, this must be tested manually (e.g. temporarily set Jwt:ExpiryMinutes=0 in a test environment and confirm 401s)."
fi

# ---------------------------------------------------------------------------
# NFR-06: a request with no Authorization header must get 401 on every
# endpoint, never 200 or 500.
# ---------------------------------------------------------------------------
section "NFR-06: missing Authorization header -> 401 everywhere"

ALL_401=1
DETAILS=""
for entry in "${PROTECTED_ENDPOINTS[@]}"; do
  IFS='|' read -r METHOD EP_PATH BODY <<< "$entry"
  CODE=$(curl -s -o /dev/null -w '%{http_code}' -X "$METHOD" "$BASE_URL$EP_PATH" \
    -H "Content-Type: application/json" \
    ${BODY:+-d "$BODY"})
  if [ "$CODE" != "401" ]; then
    ALL_401=0
    DETAILS="${DETAILS}${METHOD} ${EP_PATH} -> ${CODE}; "
  fi
done

if [ "$ALL_401" -eq 1 ]; then
  log_result "NFR-06" "PASS" "All ${#PROTECTED_ENDPOINTS[@]} sampled protected endpoints returned 401 with no Authorization header."
else
  log_result "NFR-06" "FAIL" "Endpoints not returning 401 with no Authorization header: $DETAILS"
fi

# Also assert a garbage bearer token still gets 401, not 500.
GARBAGE_CODE=$(curl -s -o /dev/null -w '%{http_code}' -X GET "$BASE_URL/api/um/mfa/status" \
  -H "Authorization: Bearer not.a.valid.jwt")
if [ "$GARBAGE_CODE" = "401" ]; then
  log_result "NFR-06-malformed" "PASS" "Malformed bearer token correctly rejected with 401 (not 500)."
else
  log_result "NFR-06-malformed" "FAIL" "Malformed bearer token returned $GARBAGE_CODE instead of 401."
fi

# ---------------------------------------------------------------------------
# NFR-07: must comply with GDPR Art. 9(2)(h) and Danish Sundhedsloven for
# health-data processing.
#
# This is a legal/organisational requirement, not something a black-box
# HTTP test can certify. The checks below only validate technical controls
# that a compliant system would be expected to have (transport security,
# origin restriction, encryption-at-rest precondition, access control on
# health-data endpoints) as supporting evidence — they do not by themselves
# prove compliance.
# ---------------------------------------------------------------------------
section "NFR-07: GDPR Art.9 / Sundhedsloven — supporting technical controls"

HSTS=$(curl -sI "$BASE_URL/api/um/ac/login" | grep -i '^strict-transport-security:' || true)
if [[ "$BASE_URL" == https://* ]]; then
  HSTS_MAXAGE=$(echo "$HSTS" | grep -oi 'max-age=[0-9]*' | grep -oE '[0-9]+' || echo "0")
  if [ -n "$HSTS" ] && [ "${HSTS_MAXAGE:-0}" -gt 0 ]; then
    log_result "NFR-07-hsts" "PASS" "HSTS header present with max-age=${HSTS_MAXAGE}s: $HSTS"
  elif [ -n "$HSTS" ]; then
    log_result "NFR-07-hsts" "FAIL" "Strict-Transport-Security header present but max-age=0, which tells browsers to STOP enforcing HTTPS-only — this disables HSTS rather than enabling it: $HSTS"
  else
    log_result "NFR-07-hsts" "FAIL" "No Strict-Transport-Security header on HTTPS deployment."
  fi
else
  log_result "NFR-07-hsts" "MANUAL" "BASE_URL is not https:// — point this script at the production HTTPS origin to check HSTS."
fi

CORS_HEADER=$(curl -s -o /dev/null -D - -H "Origin: https://evil.example.com" \
  -X OPTIONS "$BASE_URL/api/dpm/usrfet/journal" \
  -H "Access-Control-Request-Method: POST" | grep -i '^access-control-allow-origin:' || true)
if echo "$CORS_HEADER" | grep -qi "evil.example.com"; then
  log_result "NFR-07-cors" "FAIL" "CORS reflects an arbitrary Origin ($CORS_HEADER) — health-data endpoints should only be reachable from the configured frontend origin."
else
  log_result "NFR-07-cors" "PASS" "CORS does not reflect an untrusted Origin for a patient-data endpoint."
fi

# Journal endpoint requires auth at all (defence-in-depth reuses NFR-06 result).
log_result "NFR-07-access-control" "$([ "$ALL_401" -eq 1 ] && echo PASS || echo FAIL)" "Health-data endpoints require a valid bearer token (see NFR-06)."

log_result "NFR-07-legal" "MANUAL" "Legal basis (Art. 9(2)(h)), data processing agreements, breach-notification process and Sundhedsloven-specific obligations cannot be verified by an HTTP test — require documentation/DPIA review for the report."

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
section "Summary"
echo "" >> "$RESULTS_FILE"
echo "**Totals: ${PASS} PASS / ${FAIL} FAIL / ${MANUAL} MANUAL**" >> "$RESULTS_FILE"

printf "\n${C_BOLD}PASS=%d FAIL=%d MANUAL=%d${C_RESET}\n" "$PASS" "$FAIL" "$MANUAL"
echo "Full results written to $RESULTS_FILE"

[ "$FAIL" -eq 0 ]
