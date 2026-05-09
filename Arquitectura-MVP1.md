# Nido Backend — Arquitectura técnica del MVP 1

Este documento describe la base técnica definida para el primer MVP del backend de **Nido**. El objetivo de esta etapa no es maximizar la cantidad de funcionalidades, sino establecer una estructura sólida sobre la cual el producto pueda crecer con orden, mantenibilidad y capacidad de prueba.

La arquitectura se fundamenta en **.NET con Clean Architecture** y una base de datos **SQL Server**; relacionándose con el frontend a través de contratos por **HTTP**.

---

## 1. Alineación tecnológica

El backend utiliza **.NET 10** y el formato actual de soluciones de código mediante:

```txt
Nido.slnx
```

Esto mantiene al repositorio alineado con las convenciones modernas del ecosistema .NET. Se escribe todo el código en **inglés** (clases, carpetas, endpoints, contratos, tests). El concepto de negocio *hogar* se representa como `Household`.

---

## 2. Estructura y organización por capas

La arquitectura del backend separa responsabilidades en cuatro proyectos principales:

```txt
nido-backend/
├─ Nido.slnx
├─ src/
│  ├─ Nido.Api/             → expone HTTP
│  ├─ Nido.Application/     → orquesta casos de uso
│  ├─ Nido.Domain/          → modela reglas de negocio puras
│  └─ Nido.Infrastructure/  → implementa persistencia e integraciones externas
├─ tests/
│  ├─ Nido.Domain.Tests/
│  ├─ Nido.Application.Tests/
│  └─ Nido.Api.IntegrationTests/
├─ docker-compose.yml       → SQL Server para desarrollo local
└─ README.md
```

### 2.1 Nido.Domain
Representa el núcleo del negocio. No depende de HTTP, Entity Framework, SQL Server ni JWT. Se compone de Entidades (`Household`), Value Objects (`HouseholdName`) y Domain Services. Debe poder entenderse y probarse sin depender de detalles técnicos.

### 2.2 Nido.Application
Contiene los casos de uso (`CreateHouseholdHandler`) y puertos (`IHouseholdRepository`). Su responsabilidad es coordinar el flujo de aplicación sin incorporar detalles de infraestructura.

### 2.3 Nido.Infrastructure
Agrupa detalles técnicos reemplazables: persistencia con Entity Framework, repositorios concretos y configuraciones. Permite cambiar herramientas técnicas sin alterar el dominio.

### 2.4 Nido.Api
Es la capa HTTP. Contiene controllers delgados que reciben requests, invocan casos de uso y devuelven respuestas.

---

## 3. Base de Datos y Persistencia

El esquema de la base de datos se administra exclusivamente con **EF Core Migrations**. La base objetivo para el desarrollo es **SQL Server**, provista de forma local reproducible mediante la configuración de Docker Compose. La fuente de verdad del modelo persistido siempre debe vivir en EF Core.

---

## 4. Estrategia de Testing

El dominio es la parte más crítica del sistema y está completamente cubierto por tests unitarios basados en **TDD estricto**, siguiendo la estructura _Given / When / Then_.

- **`Nido.Domain.Tests` y `Nido.Application.Tests`**: Prueban clases en aislamiento puro (unitarias) reemplazando dependencias.
- **`Nido.Api.IntegrationTests`**: Validan los flujos e endpoints enteros (integración) valiédose de `WebApplicationFactory` y bases de datos in-memory o contenereizadas, sin requerir levantar dependencias externas a mano.

---

## 5. Reglas de calidad del backend

- No crear carpetas genéricas como `Utils`, `Helpers` o `Common` sin una responsabilidad concreta.
- Todo código de dominio debe tener pruebas unitarias.
- El dominio no depende de infraestructura; la infraestructura sí puede depender del dominio.
- El entorno local debe permitir levantar infra a través de Docker de forma reproducible.
- Los commits deben ser unidades de trabajo claras y sobre *feature branches*.