```markdown
## Katman Düzeni

- Domain → Entity, Value Object, Enum. Dış bağımlılık yok.
- Application → Command/Query, DTO, Validator, IRepository.
- Infrastructure → EF Core, Dapper, Redis, Identity.
- Presentation → Blazor uygulamaları, Minimal API.

## Bağımlılık Yönü
Yalnızca içe doğru. Application → Infrastructure yok; yalnız arayüz üzerinden.

## İlgili
[[MediatR Pipeline]]
[[EF Core vs Dapper]]