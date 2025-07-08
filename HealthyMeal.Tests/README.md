# HealthyMeal.Tests

Ten projekt zawiera testy jednostkowe dla aplikacji HealthyMeal.

## Struktura testów

### Services

- `PasswordHasherServiceTests.cs` - Testy serwisu hashowania haseł (BCrypt)
- `AuthServiceTests.cs` - Testy serwisu autoryzacji
- `NavigationServiceTests.cs` - Testy serwisu nawigacji

### Models

- `EntityBaseTests.cs` - Testy klasy bazowej Entity (dziedziczenie)

## Uruchomienie testów

### Przywracanie zależności

```bash
dotnet restore HealthyMeal.Tests/HealthyMeal.Tests.csproj
```

### Budowanie testów

```bash
dotnet build HealthyMeal.Tests/HealthyMeal.Tests.csproj --configuration Release
```

### Uruchomienie testów

```bash
dotnet test HealthyMeal.Tests/HealthyMeal.Tests.csproj --verbosity normal
```

### Uruchomienie testów z pokryciem kodu

```bash
dotnet test HealthyMeal.Tests/HealthyMeal.Tests.csproj --logger trx --collect:"XPlat Code Coverage"
```

## Technologie

- **Framework testów:** NUnit 3.14.0
- **Mocking:** Moq 4.20.69
- **Platform:** .NET 8.0 Windows
- **Code Coverage:** coverlet.collector

## Wyniki testów

Projekty testów zawierają:

- 17 testów jednostkowych
- Pokrycie kodu generowane do plików XML
- Wyniki testów w formacie TRX

## CI/CD

Testy są automatycznie uruchamiane przez GitHub Actions workflow w `.github/workflows/ci-cd.yml`

## Struktura wynikowa

```
HealthyMeal.Tests/
├── Services/
│   ├── AuthServiceTests.cs
│   ├── NavigationServiceTests.cs
│   └── PasswordHasherServiceTests.cs
├── Models/
│   └── EntityBaseTests.cs
├── TestResults/
│   ├── *.trx (wyniki testów)
│   └── coverage.cobertura.xml (pokrycie kodu)
└── README.md
```
