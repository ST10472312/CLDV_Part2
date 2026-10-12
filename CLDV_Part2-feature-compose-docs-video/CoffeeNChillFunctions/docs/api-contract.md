# API contract — CoffeeNChill Part 1

The endpoint specification the implementation is built against and the Postman
collection asserts on. Where this document and the code disagree, this document
defines the intended behaviour.

Status code semantics follow RFC 9110 (Fielding, Nottingham and Reschke, 2022).
Sources cited here are listed in full in the root
[`README.md`](../README.md#code-attribution-and-references).

---

## 1. Conventions

| Concern | Decision |
| --- | --- |
| Base URL | `http://localhost:7071` locally, the mapped container port otherwise. Requests reference it as `{{baseUrl}}`. |
| Route prefix | `api`, the Functions host default; `host.json` does not override `routePrefix` (Microsoft, 2026b). |
| Authorisation | `AuthorizationLevel.Anonymous` for every endpoint in Part 1. |
| Request body | `application/json` for the menu routes, `multipart/form-data` for the upload route. |
| Response body | `application/json; charset=utf-8`, except the download route, which streams the stored file. |
| Property casing | camelCase in JSON, PascalCase in C#, bridged by the worker serialiser configured with `JsonSerializerDefaults.Web` rather than by attributes on individual properties. |
| Money | `double`. Azure Table Storage has no decimal Edm type, so `decimal` cannot be persisted on an entity (Microsoft, 2026e). |
| Timestamps | ISO 8601, UTC. |

### Error shape

Every failure from every endpoint returns the same object, so a client needs one
error-handling path rather than eight.

```json
{
  "errors": [
    "Price must be greater than zero and no more than 10000.",
    "IsAvailable is required."
  ]
}
```

`errors` is always an array, including when it holds one message. Each message
names the field it refers to.

### Categories

Four values, compared with an ordinal string comparison:

`Hot Drinks` · `Cold Drinks` · `Pastries` · `Sandwiches`

The category is the `PartitionKey`. Spaces are permitted in a partition key —
the disallowed characters are `/`, `\`, `#`, `?` and control characters, with a
1024-character cap (Microsoft, 2026e) — but the value is percent-encoded in a
route, so `Hot Drinks` is addressed as `Hot%20Drinks`.

### SKU

`^[A-Z]{3}-[0-9]{3}$` — three uppercase letters, a hyphen, three digits. The SKU
is the `RowKey`, so it is unique within a category rather than globally.

---

## 2. Endpoints

| # | Route | Method | Success | Failure |
| --- | --- | --- | --- | --- |
| 1 | `/api/menu` | POST | 201 | 400, 409 |
| 2 | `/api/menu` | GET | 200 | — |
| 3 | `/api/menu/category/{category}` | GET | 200 | 400 |
| 4 | `/api/menu/{category}/{id}` | PUT | 200 | 400, 404 |
| 5 | `/api/menu/{category}/{id}` | DELETE | 204 | 404 |
| 6 | `/api/documents/upload` | POST | 201 | 400 |
| 7 | `/api/documents` | GET | 200 | — |
| 8 | `/api/documents/download/{fileName}` | GET | 200 | 404 |

Any route outside this table returns 404 from the Functions host.

---

## 3. Menu endpoints

### 3.1 `POST /api/menu`

Request:

```json
{
  "category": "Hot Drinks",
  "sku": "COF-001",
  "name": "Espresso",
  "description": "Double shot, single origin.",
  "price": 22,
  "isAvailable": true
}
```

| Field | Type | Required | Rule |
| --- | --- | --- | --- |
| `category` | string | yes | One of the four categories |
| `sku` | string | yes | `ABC-123` pattern |
| `name` | string | yes | 2–100 characters |
| `description` | string | no | ≤ 250 characters |
| `price` | number | yes | > 0 and ≤ 10000 |
| `isAvailable` | boolean | yes | No default; omission is a validation error rather than `false` |

Responses:

| Status | Condition | Body |
| --- | --- | --- |
| 201 | Created | The stored item, with a `Location` header |
| 400 | Body is not valid JSON, is empty, or fails validation | `errors` |
| 409 | The SKU already exists in that category | `errors` naming the SKU |

201 body:

```json
{
  "category": "Hot Drinks",
  "sku": "COF-001",
  "name": "Espresso",
  "description": "Double shot, single origin.",
  "price": 22,
  "isAvailable": true
}
```

`PartitionKey`, `RowKey`, `Timestamp` and `ETag` are storage concerns and do not
appear in responses. The category and SKU are returned under their business
names.

### 3.2 `GET /api/menu`

200 with a JSON array of the object above. An empty table returns `[]`.

### 3.3 `GET /api/menu/category/{category}`

| Status | Condition |
| --- | --- |
| 200 | The category is one of the four. Returns an array, possibly empty. |
| 400 | The category is not one of the four. |

An empty category and an unknown category are distinct outcomes. `Sandwiches`
containing no items is a valid category that happens to be empty, so it returns
**200 with `[]`**. `Soup` is not a category, so the request itself is malformed
and returns **400**.

### 3.4 `PUT /api/menu/{category}/{id}`

Request — at least one field, each individually optional:

```json
{ "price": 26.5, "isAvailable": false }
```

| Status | Condition | Body |
| --- | --- | --- |
| 200 | Updated | The full item with the new values |
| 400 | Body is not valid JSON, is empty, carries neither field, or fails validation | `errors` |
| 404 | No such SKU in that category | `errors` |
| 409 | The row changed between the read and the write (ETag mismatch) | `errors` |

Body validation precedes the resource lookup, so a malformed or empty body
returns 400 even when the SKU does not exist.

Fields not supplied retain their stored values. `name` and `description` are not
editable through this route in Part 1.

### 3.5 `DELETE /api/menu/{category}/{id}`

| Status | Condition | Body |
| --- | --- | --- |
| 204 | Deleted | None |
| 404 | No such SKU in that category | `errors` |

`TableClient.DeleteEntityAsync` does not throw when the entity is absent; the
SDK returns the response for both 404 and 204 and throws only for other statuses
(Azure SDK for .NET, 2026). The 404 case is therefore determined by inspecting
`Response.Status` rather than by catching `RequestFailedException`.

---

## 4. Document endpoints

`staff-docs` is backed by a blob container rather than an Azure File Share,
because Azurite emulates only the Blob, Queue and Table services (Microsoft,
2026g). The routes, status codes, guards and metadata in this section are
unaffected by that choice. The full reasoning is in the README.

### 4.1 `POST /api/documents/upload`

`multipart/form-data` with a single file part named `file`.

| Guard | Limit |
| --- | --- |
| Allowed MIME types | `application/pdf`, `image/png`, `image/jpeg`, `text/plain` |
| Maximum size | 5 MB (5 242 880 bytes) |
| File part | Required; a multipart body with no file part returns 400 |

| Status | Condition | Body |
| --- | --- | --- |
| 201 | Stored | Metadata object below |
| 400 | Disallowed type, oversized, or no file part | `errors` |

201 body:

```json
{
  "fileName": "barista-recipe-sheet.pdf",
  "sizeBytes": 48213,
  "contentType": "application/pdf",
  "lastModified": "2026-09-13T10:15:00Z"
}
```

### 4.2 `GET /api/documents`

200 with an array of the metadata object above. File name, size and last
modified date are present on every entry.

### 4.3 `GET /api/documents/download/{fileName}`

| Status | Condition | Body |
| --- | --- | --- |
| 200 | Found | The file bytes |
| 404 | No such file in `staff-docs` | `errors` |

A 200 response carries:

- `Content-Type` matching the stored type, for example `application/pdf`
- `Content-Disposition: attachment; filename="<the file name>"`

---

## 5. Reserved test data

| Reserved | Reason |
| --- | --- |
| `Hot Drinks / COF-001` | Created and deleted by every run of the Postman collection, which is what makes the run repeatable. `seed-menu.json` does not contain it. |
| `Sandwiches` | Left empty by `seed-menu.json` so the empty-category case has something to assert against. |

`seed-menu.json` populates Hot Drinks, Cold Drinks and Pastries only.
