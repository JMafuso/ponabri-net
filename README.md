# Projeto Ponabri API

## Visão Geral
A API Ponabri é uma aplicação RESTful desenvolvida em ASP.NET Core para gerenciamento de abrigos temporários e reservas. O projeto utiliza Oracle como banco de dados, integrações com RabbitMQ para mensageria e ML.NET para categorização inteligente de abrigos.

## Funcionalidades Implementadas

### 1. Requisitos da Disciplina .NET Atendidos
- CRUD completo para três entidades principais: Usuários, Abrigos e Reservas, utilizando Entity Framework Core com Oracle.
- Boas práticas RESTful implementadas, incluindo uso de DTOs para validação e segurança, HATEOAS para navegação via links, e Rate Limiting configurado e ativado.
- Documentação completa via Swagger UI, refletindo todos os endpoints e DTOs.
- Integração com RabbitMQ para publicação de eventos assíncronos, especialmente na criação de reservas.
- Uso de ML.NET para categorização automática de abrigos com base na descrição.
- Segurança robusta com autenticação JWT e hashing seguro de senhas com BCrypt.

### 2. Principais Funcionalidades por Controller
- **UsuariosController**: Registro de usuários com hashing de senha, login com geração de token JWT, e endpoints CRUD protegidos por roles.
- **AbrigosController**: CRUD completo com paginação, integração com ML para sugestão de categoria, controle de vagas e status dos abrigos.
- **ReservasController**: Criação e cancelamento de reservas com verificação e atualização de vagas, validação de reservas para IoT, publicação de mensagens no RabbitMQ, e endpoints protegidos.

### 3. Boas Práticas Implementadas
- Uso consistente de DTOs para entrada e saída, garantindo validação e segurança.
- Implementação de HATEOAS para facilitar a navegação e descoberta dos recursos da API.
- Rate Limiting configurado para proteção contra abuso e ataques de negação de serviço.
- Tratamento padronizado de erros e respostas.

### 4. Integrações
- RabbitMQ para comunicação assíncrona e eventos.
- ML.NET para categorização inteligente de abrigos.

### 5. Segurança
- Autenticação e autorização via JWT.
- Hashing de senhas com BCrypt para proteção das credenciais dos usuários.

### 6. Documentação
- Swagger UI disponível para testes e visualização da API.

## Testes e Validação
- Rate Limiting ativado e testado com sucesso, limitando requisições conforme configurado.
- Recomenda-se a realização de testes automatizados para garantir a robustez da API.

## Como Testar o Rate Limiting via Swagger
1. Acesse o Swagger UI da API.
2. Selecione um endpoint, por exemplo, `GET /api/Abrigos`.
3. Execute várias requisições rápidas (mais de 5 por segundo).
4. Observe que, ao ultrapassar o limite, a API retorna o status HTTP 429 (Too Many Requests).

## Próximos Passos
- Implementar testes automatizados para os principais fluxos.
- Revisar e documentar fluxos de erro para melhor experiência do cliente.
- Avaliar melhorias de performance e segurança adicionais.

## Informações para Testes

Para realizar testes autenticados no swagger, utilize o seguinte usuário administrador:

- Email: admin@admin.com
- Senha: Admin!123

Use essas credenciais para obter o token JWT via endpoint `POST /api/Usuarios/login` e autorizar requisições protegidas, como `GET /api/Usuarios`.

