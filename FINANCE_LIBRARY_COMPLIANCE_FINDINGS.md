# Compliance findings for the financial-domain library

**Status:** external facts captured for design; not legal or tax advice.
**As of:** 2026-08-19

## Verified external constraints

The Ministry for Taxes and Duties of the Republic of Belarus states that a payer using the professional income tax regime must use the official digital platform/application. Information about a calculation is transferred by forming a receipt in that application. The receipt is formed on each payment event; for card, QR, mobile-app, cashless and electronic-money payments, the published guidance allows the receipt to be formed no later than the 7th day of the following month. The published page does not document a public server-to-server API that a third-party library can invoke to issue that receipt automatically. Therefore this library must expose a **tax-receipt submission port** and must not implement an unverified direct MNS API client. [1]

The published 60,000 BYN annual threshold applies to income from Belarusian organisations and individual entrepreneurs registered with Belarusian tax authorities; it does not appear as a universal ceiling for all professional income. Foreign organisations and foreign individual entrepreneurs are listed under the 10% rate regardless of amount. Therefore the library must classify the counterparty and calculate an alerting policy from configured tax rules rather than block all payouts on an assumed universal 60,000 BYN limit. [1]

From 1 July 2026, the official MNS notice states a monthly minimum tax of 45 BYN (18 BYN for pension recipients) and reports removal from the regime after three consecutive late payments, with re-registration available after seven months. These values are date-sensitive policy data, not domain constants. [2]

## Architecture consequences

| Requirement | Library decision |
|---|---|
| Tax rule changes over time | `ITaxPolicy` port / effective-dated policy value objects; no rate or threshold hard-coding. |
| Official MNS app is required | `ITaxReceiptGateway` application port; manual and authorised provider adapters are both possible. |
| Receipt timing follows the payment event, not only a later bank withdrawal | `TaxableReceiptCandidate` is created from a confirmed payment/settlement event, while payout remains a separate aggregate. |
| Incoming payment amount, provider fee and bank fee are distinct | `MoneyMovement` separates gross, provider fee, bank fee and net; tax base is returned by a configurable tax policy, never inferred from net payout. |
| Payout buffering has regulatory and contract risk | Ledger holds are represented as provider-owned funds and require an explicit settlement status; no code path models personal discretionary “hiding” of income. |

## References

[1]: https://www.nalog.gov.by/professional_income_tax/ "МНС Республики Беларусь — Налог на профессиональный доход"
[2]: https://nalog.gov.by/actual/npd_45/ "МНС Республики Беларусь — НПД с 01.07.2026"

## Payment-provider finding

The public EasyStaff Invoice page describes a balance-based user flow: after verification, a user creates an invoice; after the payment reaches the provider account, it appears in the provider balance and the user can order a payout. The same page distinguishes card-link and bank-transfer timing, documents matching requirements between invoice amount/sender and incoming payment, and does not expose public API or webhook documentation in the reviewed material. Accordingly, `IPaymentProviderWebhookVerifier` and `IPaymentProviderPort` remain generic provider ports; an EasyStaff adapter is intentionally **not** implemented until credentials and official integration documentation are supplied. [3]

[3]: https://easystaff.io/ru/invoice "EasyStaff Invoice — официальный русскоязычный продуктовый материал"

## Bank-fee finding

The reviewed MTBank tariff page presents time-bounded tariff documents and the reviewed 2024 notice describes fees for specific card cash-withdrawal and outbound-transfer operations, not a universal 3% fee for the requested incoming payout route. `IBankFeeSchedule` must therefore be an effective-dated external port/configuration; the 3% figure from the backlog is retained only as a scenario input in tests and never as a production domain constant. [4]

[4]: https://www.mtbank.by/about/rates/ "МТБанк — действующие тарифы"
