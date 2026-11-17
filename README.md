# Betty Casino – Wallet & Game services

A small casino platform in .NET 10 made of two independently running services, a **Wallet** and a **Game**, behind a thin **API gateway**, with a console client to play.

## How to run

**Prerequisites:** .NET 10 SDK and Docker (running).

```bash
# 1. Start everything: Postgres, Wallet, Games, Gateway (+ Aspire dashboard)
dotnet run --project Casino.AppHost

# 2. In a second terminal, start the client
dotnet run --project Casino.Client
# or point it at a specific gateway URL
dotnet run --project Casino.Client -- https://localhost:7029/
```

The gateway URL can also be set with the `CASINO_GATEWAY_URL` environment variable. The Aspire dashboard (its URL is printed by the AppHost) shows every resource, its logs, and distributed traces of each request across gateway → Games → Wallet.

> The client is not started by Aspire on purpose: an interactive console started by Aspire gets no keyboard input.
> If HTTPS complains about the certificate, run `dotnet dev-certs https --trust` once, or use the gateway's `http://` URL.

## How to play

Enter a player name. The same name always brings you back to the same wallet, and a new name starts at $0.

| Command | Example |
|---|---|
| `deposit <amount>` | `deposit 10` |
| `withdraw <amount>` | `withdraw 13.50` |
| `bet <amount>` | `bet 5` (between $1 and $10) |
| `balance` | `balance` |
| `exit` | `exit` |

```
> deposit 10
Your deposit of $10 was successful. Your current balance is: $10

> bet 5
Congrats - you won $8.50! Your current balance is: $13.50

> withdraw 13.50
Your withdrawal of $13.50 was successful. Your current balance is: $0

> exit
Thank you for playing! Hope to see you again soon.
```

## Architecture

```
                 ┌──────────────┐
  Console  ────► │   Gateway    │  YARP, routing only, exposes /api/players/* and /api/bets/*
  client         └──────┬───────┘
                        │
           ┌────────────┴─────────────┐
           ▼                          ▼
   ┌───────────────┐   POST     ┌───────────────┐
   │   Games API   │ ─────────► │  Wallet API   │
   │ rules, rounds │ /internal/ │ money, ledger │
   └───────┬───────┘  rounds    └───────┬───────┘
           ▼                            ▼
       games db                     wallet db       (one Postgres container, two databases)
```

- **Wallet** is the only source of truth for money. It owns players and accounts, and every balance change is an entry in an append-only ledger. It knows nothing about games.

- **Games** owns the game rules (bet limits, outcome distribution) and the rounds. It never stores balances. It asks Wallet to settle each round.

- **Gateway** is the single entry point for the client. It routes only the public endpoints. The internal settle-round endpoint is not routed, so the client cannot credit itself winnings.

- **.NET Aspire** runs everything locally and provides service discovery, Postgres, retries/timeouts (Polly) and OpenTelemetry.

## How a round works

1. Games validates the bet, **decides the outcome**, and saves the round as **Pending**, before any money moves.
2. Games makes **one** call to Wallet, which debits the bet and credits the win **atomically in one transaction**, or rejects the round if the balance is too low.
3. Games marks the round **Settled** or **Rejected** and returns the new balance.

**When something fails** (Wallet times out, errors, or Games crashes mid-bet), the round stays Pending and the client gets `202 Accepted`. A background worker in Games retries Pending rounds every few seconds. Retrying is always safe: Wallet uses the `roundId` as an idempotency key, so a round is settled exactly once, and the stored outcome means a retry can never "re-roll" it.

## Screenshots

<img width="3452" height="2156" alt="image" src="https://github.com/user-attachments/assets/710b368f-75da-4431-a762-dfac286c43ab" />
<img width="3452" height="2156" alt="image" src="https://github.com/user-attachments/assets/90c0eab9-cf3c-427e-8087-cbf48fbde5f0" />


