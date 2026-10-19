# Data and ML: <name>

- **Status**: draft | approved (YYYY-MM-DD)
- **Spec**: [spec.md](spec.md) · **Architecture**: [architecture.md](architecture.md)

## 1. Data model
<!-- Entities, relationships (Mermaid erDiagram), keys, indexes, constraints, volumes and growth. -->

## 2. Data classification and privacy
| Field / entity | Class (public / internal / personal / sensitive) | Protection | Retention | Deletion |
|---|---|---|---|---|

## 3. Migrations and lifecycle
<!-- Migration tool and policy, zero-downtime rules, seed/test data (synthetic only), backups (with sre). -->

## 4. Pipelines
| Pipeline | Source → sink | Schedule / trigger | Idempotent? | Quality checks | Failure handling |
|---|---|---|---|---|---|

## 5. ML / AI features
<!-- Problem framing, baseline, model choice and why, data/labels, prompt and retrieval design, guardrails. -->

### Evaluation
| Eval set | Size | Metric | Threshold | Latest score | Date |
|---|---|---|---|---|---|

### Cost
<!-- Cost per request / per month at expected usage, and limits. -->

## 6. Analytics
<!-- Events, properties, consent, where they go. -->
