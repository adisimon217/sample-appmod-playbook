# ModNet — .NET & SQL Server Modernization Framework

Structured framework for AWS partners to assess, plan, and execute modernization of Windows workloads (.NET + SQL Server) to AWS.

## Framework Flow

```mermaid
graph TD
    P1["100 · Data Collection"]
    P2["200 · Mod Feasibility"]
    P3["300 · Detailed Assessment + Architecture"]
    P4["400 · Mod Plan + Execution"]

    P1 -->|inventory| P2
    P2 -->|wave plan| P3
    P3 -->|assessment| P4

    style P1 fill:#495057,stroke:#343a40,color:#fff
    style P2 fill:#495057,stroke:#343a40,color:#fff
    style P3 fill:#5a6c7d,stroke:#3d4f5f,color:#fff
    style P4 fill:#2d6a4f,stroke:#1b4332,color:#fff
```

## Available Skills

Skills are the primary way to interact with this framework. Each skill guides you through a specific phase with step-by-step instructions. Templates, analysis rules, and sample outputs are embedded in each skill's `references/` folder.

| Skill | Phase | Description |
|-------|-------|-------------|
| `start-engagement` | Setup | Create the working folder structure, copy templates, determine engagement type |
| `data-collection` | 100 | Guide portfolio inventory collection using the App Inventory spreadsheet |
| `feasibility-analysis` | 200 | Classify, score, and prioritize applications into modernization waves |
| `detailed-assessment` | 300 | Per-application deep dive: questionnaires, architecture workshops, iterative refinement |
| `generate-plans` | 400 | Produce PoC, EBA, and full modernization project plans for all applications |
| `excel-analysis` | Utility | Read and analyze Excel spreadsheets (used by other skills for inventory data) |

Invoke any skill by name (e.g., "run the data-collection skill" or "invoke generate-plans"). Skills are designed to be invoked in sequence, but each can also be run independently if prerequisites are met.

## Getting Started

To start a new customer engagement, invoke the `start-engagement` skill and provide the customer name. The skill will create the folder structure, copy templates, ask about the engagement type, and guide you on next steps.

Once the engagement is set up, invoke skills in order: `data-collection` -> `feasibility-analysis` -> `detailed-assessment` -> `generate-plans`. Each skill picks up where the previous one left off.

## Target State

| Layer | Primary | Alternatives |
|-------|---------|-------------|
| Application | ECS Fargate (Linux), .NET 10 | EKS, Lambda, EC2 Linux |
| Database | Aurora PostgreSQL | RDS SQL Server Standard, DynamoDB |
| Caching | ElastiCache Valkey | ElastiCache Redis |
| Stretch | CI/CD (CodePipeline), Observability (OpenTelemetry + CloudWatch) | |

## Key Tools

| Tool | Role |
|------|------|
| [AWS Transform for .NET](https://docs.aws.amazon.com/transform/latest/userguide/dotnet.html) | .NET Framework to cross-platform .NET |
| [AWS Transform for SQL Server](https://docs.aws.amazon.com/transform/latest/userguide/sql-server-modernization.html) | SQL Server to Aurora PostgreSQL |
| [AWS Transform Custom](https://docs.aws.amazon.com/transform/latest/userguide/custom.html) | Web Forms to MVC, repeatable patterns at scale |
| [Kiro](https://kiro.dev/docs/) | AI IDE for analysis, porting, and complex rewrites |

Note: This playbook is meant to be a reference guide - not intended for production use.

## License

[MIT License](LICENSE)

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
