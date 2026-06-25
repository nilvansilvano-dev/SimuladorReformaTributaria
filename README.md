# BaruchGest — Simulador de Impacto da Reforma Tributária IBS/CBS

> Ferramenta profissional para análise do impacto da Reforma Tributária (LC 214/2025) nos regimes fiscais brasileiros.

---

## Visão Geral

O **BaruchGest Simulador** calcula e compara o impacto da Reforma Tributária (IBS/CBS) nos quatro principais regimes fiscais:

- Lucro Real
- Lucro Presumido
- Simples Nacional — COM crédito IBS/CBS
- Simples Nacional — SEM crédito IBS/CBS

O sistema possui autenticação por login, painel administrativo para gerenciar usuários, exportação em PDF, página informativa sobre a LC 214/2025 e assistente de IA especializado em reforma tributária.

---

## Estrutura do Projeto

```
C:\Atividades-SMN-New\
│
├── BaruchGest.Api\                  ← Backend (ASP.NET Core 10 Web API)
│   ├── Controllers\
│   │   ├── AuthController.cs        ← Login e cadastro
│   │   ├── AdminController.cs       ← Gestão de usuários
│   │   ├── SimuladorController.cs   ← Cálculo da reforma (POST /api/simular)
│   │   └── ChatController.cs        ← IA Gemini (POST /api/chat)
│   ├── Calculadora\
│   │   ├── Empresa.cs               ← Modelo de dados da empresa
│   │   └── Calculadora.cs           ← Lógica de cálculo dos 4 regimes
│   ├── Data\
│   │   └── AppDbContext.cs          ← EF Core + SQLite
│   ├── Models\
│   │   ├── Usuario.cs               ← Entidade de usuário
│   │   └── Dtos.cs                  ← DTOs de request/response
│   ├── Services\
│   │   └── AuthService.cs           ← Geração de token JWT
│   ├── appsettings.json             ← Configurações (JWT, banco, Gemini)
│   ├── Program.cs                   ← Bootstrap da API
│   └── baruchgest.db                ← Banco SQLite (gerado automaticamente)
│
└── CalculoContabilidade\
    ├── frontend\                    ← Frontend (HTML + CSS + JS puro)
    │   ├── index.html               ← Simulador principal
    │   ├── login.html               ← Tela de login e cadastro
    │   ├── admin.html               ← Painel administrativo
    │   ├── lei214.html              ← Página informativa LC 214/2025
    │   ├── style.css                ← Estilos (paleta BaruchGest)
    │   ├── app.js                   ← Lógica do simulador
    │   ├── chat.js                  ← Widget de IA (flutuante)
    │   ├── build.js                 ← Script de ofuscação para deploy
    │   ├── package.json
    │   └── dist\                    ← Versão ofuscada para produção
    │
    ├── Empresa.cs                   ← (Aplicativo console original)
    ├── Calculadora.cs
    ├── Program.cs
    └── CalculoContabilidade.csproj
```

---

## Tecnologias

| Camada | Tecnologia |
|---|---|
| Backend | ASP.NET Core 10, C# |
| Banco de dados | SQLite via Entity Framework Core 10 |
| Autenticação | JWT Bearer Token |
| Hash de senha | BCrypt.Net |
| IA | Google Gemini 1.5 Flash (gratuito) |
| Frontend | HTML5, CSS3, JavaScript puro (sem frameworks) |
| Ofuscação | javascript-obfuscator (Node.js) |

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Navegador moderno (Chrome, Edge, Firefox)
- *(Opcional)* Node.js — somente para gerar build ofuscado para produção

---

## Como Executar

### 1. Iniciar a API (Backend)

Abra um terminal e execute:

```powershell
cd C:\Atividades-SMN-New\BaruchGest.Api
dotnet run --launch-profile http
```

Aguarde a mensagem:
```
Now listening on: http://localhost:5000
Application started.
```

> **Mantenha esse terminal aberto** enquanto usa o sistema.

### 2. Abrir o Frontend

Abra o arquivo diretamente no navegador:

```
C:\Atividades-SMN-New\CalculoContabilidade\frontend\login.html
```

---

## Credenciais Padrão (Admin)

```
Email:  admin@baruchgest.com.br
Senha:  BaruchGest@2025
```

> Recomendado: altere a senha após o primeiro acesso.

---

## Endpoints da API

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| `POST` | `/api/auth/login` | ❌ | Fazer login |
| `POST` | `/api/auth/registrar` | ❌ | Solicitar acesso |
| `POST` | `/api/simular` | ✅ JWT | Calcular impacto da reforma |
| `POST` | `/api/chat` | ✅ JWT | Consultar assistente IA |
| `GET`  | `/api/admin/usuarios` | ✅ Admin | Listar usuários |
| `PUT`  | `/api/admin/usuarios/{id}/toggle` | ✅ Admin | Liberar ou bloquear usuário |
| `DELETE` | `/api/admin/usuarios/{id}` | ✅ Admin | Excluir usuário |

---

## Fluxo de Acesso

```
Usuário → login.html → cadastra-se → conta INATIVA
Admin   → admin.html → clica "Liberar" → conta ATIVA
Usuário → faz login  → acessa o simulador
```

---

## Configurar Assistente IA (Gemini)

1. Acesse [aistudio.google.com/app/apikey](https://aistudio.google.com/app/apikey)
2. Crie uma chave gratuita
3. Abra `BaruchGest.Api\appsettings.json` e substitua:

```json
"Gemini": {
  "ApiKey": "COLE_SUA_CHAVE_AQUI"
}
```

4. Reinicie a API

**Limites gratuitos:** 1.500 requisições/dia · 1 milhão de tokens/dia

---

## Gerar Build para Produção (Código Ofuscado)

O script ofusca o `app.js` para proteger a lógica do simulador:

```powershell
cd C:\Atividades-SMN-New\CalculoContabilidade\frontend
npm install       # somente na primeira vez
npm run build
```

A pasta `frontend\dist\` conterá os arquivos prontos para upload no servidor.

---

## Paleta de Cores — Marca BaruchGest

| Cor | Hex | Uso |
|---|---|---|
| Azul escuro | `#0F172A` | Fundo do header, botões principais |
| Verde esmeralda | `#10B981` | Destaques, bordas, ações positivas |
| Branco | `#FFFFFF` | Fundos de card, textos em fundo escuro |

---

## Regimes Fiscais Calculados

### Lucro Real
Cálculo com IRPJ/CSLL sobre o lucro real apurado. Considera a eliminação dos tributos extintos (ICMS, ISS, PIS, COFINS) tanto na receita quanto nos custos.

### Lucro Presumido
Base do IRPJ/CSLL não muda com a reforma (calculada sobre receita bruta total incluindo IBS/CBS). Custos e despesas são recalculados com os novos parâmetros.

### Simples Nacional COM Crédito IBS/CBS
ISS/PIS/COFINS saem do DAS após a reforma. IBS/CBS cobrado separado (fora do DAS). Empresa pode se creditar do IBS/CBS das compras.

### Simples Nacional SEM Crédito IBS/CBS
IBS/CBS permanece dentro do DAS (alíquota menor pela tabela). Porém, sem direito a crédito das compras — custos aumentam.

---

## Reforma Tributária — Resumo

| Imposto atual | Substituído por | Extinção |
|---|---|---|
| PIS + COFINS | CBS (federal) | 2027 |
| ICMS | IBS (estados/municípios) | 2029–2033 |
| ISS | IBS (estados/municípios) | 2029–2033 |
| IPI | IS (Imposto Seletivo) | 2027–2033 |

**Alíquota estimada IBS + CBS:** ~28%  
**Split Payment:** imposto separado automaticamente pelo sistema financeiro no momento do pagamento.

---

## Desenvolvido por

**BaruchGest** — Transformando controle em crescimento.
