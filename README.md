# Sistema de eventos com venda de ingresso usando API do Mercado Pago

• Aplicativo de loja Virtual Web Crud feito com C# ASP.NET CORE

• Banco de dados SQL SERVER

• ASP.NET Core Identity
 
• .NET 8
 
• SMTP Gmail

• Data Annotations

• Entity Framework

• Razor Pages

• Tailwind

• Mercado Pago SDK

Usuário administrador padrão criado automaticamente:

Email: admin@evento.com
Senha: Admin123!

# Banco de dados SQL SERVER
Comandos pacote NuGet (Entity framework)


  Importante: Mude o Server do DefaultConnection no appsettings.json, para o nome do servidor de sua máquina e com a senha caso necessário

  -Realizar os seguintes comandos ou criar o banco manualmente:
  
  • Add-migration Migrations (Criar banco de dados)

  • Update-Database (atualizar banco de dados)

# Importante:
Para alteração de senha funcionar por Envio de email na aba de Login, você deve mudar o:

```
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "Seu email aqui",
    "Password": "Sua senha codificada aqui"
```
no appsettings.Development.json, coloque o email que deseja que envie o email para a redefinição de Senha para o usuario, para isso o email desejado deve ativar a verificação em duas etapas e ir em https://myaccount.google.com/apppasswords, Na seção "Selecionar app", escolha "Mail". Vai aparecer uma senha como: abcd efgh ijkl mnop (sem espaços — copie inteira), e cole no "Password", isso porque o Gmail não aceita mais sua senha normal em conexões SMTP com apps externos. É obrigatório usar uma "senha de app" se você tem autenticação em 2 etapas (2FA) ativada

# Api Mercado Pago SDK:
 Antes de usar a API, você precisa:

 • Criar uma conta no Mercado Pago

 • Criar uma aplicação no painel de desenvolvedor

Depois copie suas chaves:

 • Public Key

 • Access Token

 #  É necessário mudar o MercadoPagoConfig.AccessToken e colocar seu Acess Token localizado no Program.cs, e mudar a Public key localizada no script do Pagamento.cshtml

 ```
 MercadoPagoConfig.AccessToken = "########";

 const mp = new MercadoPago("SUA-PUBLIC-TOKEN-AQUI", {
     locale: "pt-BR"
 });
```
