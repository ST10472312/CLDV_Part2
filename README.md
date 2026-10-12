# CLDV6212_part2

## Member 2 — Queue Consumer & Orders Table (ST10472017)

Everything after the message lands on `order-processing-queue`.

`ProcessOrderQueue` fires on each queue message, parses the JSON payload and writes
a row to the `Orders` table (`PartitionKey` = order date, `RowKey` = OrderId,
`Status` = `Received`). Writes are idempotent: queues deliver at least once, so a
redelivered message finds the existing row and is treated as success rather than
failing on a duplicate key.

The row then moves through `Received → Preparing → Ready → Collected`, one step every
`STATUS_DELAY_SECONDS`. Status updates are ETag-checked, so a manual change through the
API stops the simulation instead of overwriting it.

`host.json` sets `maxDequeueCount` to 3. A message that keeps failing is retried three
times and then moved to `order-processing-queue-poison`, where `HandlePoisonOrder` logs
it and records the order with status `Failed`.

### Endpoints

| Method | Route | Success | Failure |
| --- | --- | --- | --- |
| GET | `/api/orders` | 200 | 400 on an unknown `?status=` filter |
| GET | `/api/orders/{orderId}` | 200 | 404 |
| PUT | `/api/orders/status` | 200 | 400, 404, 409 |

A status may only move to the step directly after the current one; anything else
returns 409.

### Settings

| Setting | Meaning |
| --- | --- |
| `STATUS_DELAY_SECONDS` | Seconds between simulated barista steps. `0` turns the simulation off. |
| `ENABLE_POISON_TEST` | When `true`, an order whose `CustomerName` is `POISON-TEST` fails deliberately, to demonstrate the poison queue. |

### Tests

`docs/wip/member2_orders_processing.postman_collection.json` holds 15 requests carrying
28 assertions across the status lifecycle, the manual transition rules, the error codes
and the poison queue.

```bash
newman run docs/wip/member2_orders_processing.postman_collection.json \


## Member 3 — Docker Compose, Docker Hub & Documentation

### Docker Compose Configuration

The project uses `docker-compose.yml` to run the CoffeeNChill Azure Functions application and Azurite storage emulator together. Docker Compose connects both services through a dedicated bridge network, allowing the Functions application to communicate with Azurite using the service hostname. A named Docker volume is configured to persist Azurite data.

The configuration was validated successfully using:

```bash
docker compose config
```

### Running the Application

The application can be started from the repository root using:

```bash
docker compose up -d
```

The running containers can be checked using:

```bash
docker compose ps
```

To view the Azure Functions application logs:

```bash
docker logs coffeenchill-functions --tail 100
```

To stop the services, run:

```bash
docker compose down
```

### Docker Hub Images

The project uses the following Docker Hub images:

- **Azure Functions:** `st10450884/coffeenchill-functions:v2.0`
- **Azurite storage emulator:** `st10450884/coffeenchill-azurite:v2.0`

The Docker Compose configuration references these images to run the application services. The project also maintains the `v1.0` and `v2.0` image versions where applicable.

### Verification and Testing

The Docker Compose configuration was validated successfully, and both containers started successfully.

The following results were verified:

- The Azurite container started and reported a healthy status.
- The CoffeeNChill Functions container started and remained running.
- The Functions host loaded nine functions: `CreateMenuItem`, `DeleteMenuItem`, `DownloadStaffDocument`, `GetAllMenuItems`, `GetMenuItemsByCategory`, `ListStaffDocuments`, `QueueOrder`, `UpdateMenuItem` and `UploadStaffDocument`.
- The application exposed its HTTP endpoints through port `7071`.
- The available API endpoints were identified from the Functions host logs for testing through Postman.

### Member 3 Contribution

Member 3 was responsible for configuring Docker Compose, integrating the Azure Functions application with the Azurite storage emulator, configuring the Docker network and persistent storage, publishing and versioning the Docker Hub images, verifying container startup and documenting the deployment and execution procedures.
  -e docs/wip/member2_orders.postman_environment.json

