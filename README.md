# aspnetcore10-opentelemetry-grafana-tempo-loki-prometheus-postgres_contagemacessos-consumobackends
 Exemplo de uso de OpenTelemetry + Grafana + Tempo (trace) + Loki (logs) + Prometheus (métricas) em uma API REST de contagem de acessos baseada em .NET 10 + ASP.NET Core e que utiliza uma base de dados PostgreSQL. Inclui um script do Docker Compose para criação do ambiente de testes + script do k6 para testes de carga.

Repositorios com as aplicacoes em outras stacks utilizadas nos testes com tracing distribuido:
- [API REST criada com Node.js](https://github.com/renatogroffe/nodejs-otel_apiconsumobackend)
- [API REST criada com Java + Spring + Apache Camel](https://github.com/renatogroffe/java-spring-camel_apiconsumobackend)
