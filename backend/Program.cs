using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

builder.Services.ConfigureHttpJsonOptions(o => {
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddCors(c => c.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

// ─── Config ──────────────────────────────────────────────────────────────────
const string JwtSecret = "baruchgest-reforma-tributaria-jwt-secret-2024";
const string DataDir   = "data";
const string UsersFile = "data/usuarios.json";

Directory.CreateDirectory(DataDir);

// ─── Serialização de arquivo ─────────────────────────────────────────────────
JsonSerializerOptions FileJson = new() {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
};

// ─── Usuários ─────────────────────────────────────────────────────────────────
List<Usuario> CarregarUsuarios() {
    if (!File.Exists(UsersFile)) return [];
    try { return JsonSerializer.Deserialize<List<Usuario>>(File.ReadAllText(UsersFile), FileJson) ?? []; }
    catch { return []; }
}

void SalvarUsuarios(List<Usuario> users) =>
    File.WriteAllText(UsersFile, JsonSerializer.Serialize(users, FileJson));

int ProximoId(List<Usuario> users) =>
    users.Count == 0 ? 1 : users.Max(u => u.Id) + 1;

// ─── Crypto ───────────────────────────────────────────────────────────────────
static string HashSenha(string senha) {
    var salt = RandomNumberGenerator.GetBytes(16);
    var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(senha), salt, 10_000, HashAlgorithmName.SHA256, 32);
    return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
}

static bool VerificarSenha(string senha, string stored) {
    var parts = stored.Split(':');
    if (parts.Length != 2) return false;
    var salt       = Convert.FromBase64String(parts[0]);
    var storedHash = Convert.FromBase64String(parts[1]);
    var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(senha), salt, 10_000, HashAlgorithmName.SHA256, 32);
    return CryptographicOperations.FixedTimeEquals(hash, storedHash);
}

// ─── JWT ──────────────────────────────────────────────────────────────────────
static string B64Url(byte[] data) =>
    Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

static byte[] B64UrlDecode(string s) {
    s = s.Replace('-', '+').Replace('_', '/');
    s += new string('=', (4 - s.Length % 4) % 4);
    return Convert.FromBase64String(s);
}

string CriarToken(int id, string email, string nome, bool isAdmin) {
    var h = B64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" }));
    var p = B64Url(JsonSerializer.SerializeToUtf8Bytes(new {
        sub = email, id, nome, isAdmin,
        exp = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds()
    }));
    var data = $"{h}.{p}";
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(JwtSecret));
    return $"{data}.{B64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)))}";
}

(string? email, bool isAdmin) ValidarToken(HttpContext ctx) {
    var auth = ctx.Request.Headers.Authorization.FirstOrDefault();
    if (auth is null || !auth.StartsWith("Bearer ")) return (null, false);
    var token = auth[7..].Trim();
    var parts = token.Split('.');
    if (parts.Length != 3) return (null, false);
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(JwtSecret));
    if (B64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}"))) != parts[2])
        return (null, false);
    try {
        var pl  = JsonDocument.Parse(B64UrlDecode(parts[1]));
        var exp = pl.RootElement.GetProperty("exp").GetInt64();
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp) return (null, false);
        var email   = pl.RootElement.GetProperty("sub").GetString() ?? "";
        var isAdmin = pl.RootElement.TryGetProperty("isAdmin", out var ia) && ia.GetBoolean();
        return (email, isAdmin);
    } catch { return (null, false); }
}

// ─── Admin padrão ─────────────────────────────────────────────────────────────
{
    var users = CarregarUsuarios();
    if (!users.Any(u => u.IsAdmin)) {
        users.Add(new Usuario {
            Id = ProximoId(users), Nome = "Admin", Email = "admin@baruchgest.com",
            SenhaHash = HashSenha("admin123"), IsAdmin = true, Ativo = true,
            CriadoEm = DateTime.UtcNow
        });
        SalvarUsuarios(users);
        Console.WriteLine("Admin criado: admin@baruchgest.com / admin123");
    }
}

// ─── Auth ─────────────────────────────────────────────────────────────────────

app.MapPost("/api/auth/login", async (HttpContext ctx) => {
    var req = await JsonSerializer.DeserializeAsync<LoginReq>(ctx.Request.Body, FileJson);
    if (req is null || string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Senha))
        return Results.BadRequest(new { erro = "Preencha email e senha." });

    var users = CarregarUsuarios();
    var u = users.FirstOrDefault(u => u.Email.Equals(req.Email, StringComparison.OrdinalIgnoreCase));

    if (u is null || !VerificarSenha(req.Senha, u.SenhaHash))
        return Results.BadRequest(new { erro = "Email ou senha incorretos." });
    if (!u.Ativo)
        return Results.BadRequest(new { erro = "Seu acesso ainda não foi aprovado pelo administrador." });

    u.UltimoAcesso = DateTime.UtcNow;
    SalvarUsuarios(users);

    return Results.Ok(new { token = CriarToken(u.Id, u.Email, u.Nome, u.IsAdmin), nome = u.Nome, isAdmin = u.IsAdmin });
});

app.MapPost("/api/auth/registrar", async (HttpContext ctx) => {
    var req = await JsonSerializer.DeserializeAsync<RegistroReq>(ctx.Request.Body, FileJson);
    if (req is null || string.IsNullOrEmpty(req.Nome) || string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Senha))
        return Results.BadRequest(new { erro = "Todos os campos são obrigatórios." });
    if (req.Senha.Length < 6)
        return Results.BadRequest(new { erro = "Senha mínima: 6 caracteres." });

    var users = CarregarUsuarios();
    if (users.Any(u => u.Email.Equals(req.Email, StringComparison.OrdinalIgnoreCase)))
        return Results.BadRequest(new { erro = "Este email já está cadastrado." });

    users.Add(new Usuario {
        Id = ProximoId(users), Nome = req.Nome, Email = req.Email,
        SenhaHash = HashSenha(req.Senha), Ativo = false, CriadoEm = DateTime.UtcNow
    });
    SalvarUsuarios(users);
    return Results.Ok(new { mensagem = "Solicitação enviada! Aguarde a aprovação do administrador." });
});

// ─── Simulação ────────────────────────────────────────────────────────────────

app.MapPost("/api/simular", async (HttpContext ctx) => {
    var (email, _) = ValidarToken(ctx);
    if (email is null) return Results.Unauthorized();

    EmpresaReq? req;
    try { req = await JsonSerializer.DeserializeAsync<EmpresaReq>(ctx.Request.Body, FileJson); }
    catch { return Results.BadRequest(new { erro = "JSON inválido." }); }
    if (req is null) return Results.BadRequest(new { erro = "Dados inválidos." });

    var e = new Empresa {
        FatProduto              = req.FatProduto,
        FatServico              = req.FatServico,
        PctCMV                  = req.PctCMV / 100m,
        PctCSV                  = req.PctCSV / 100m,
        PctDespesas             = req.PctDespesas / 100m,
        PctDirCredito           = req.PctDirCredito / 100m,
        AliqIbsCbs              = req.AliqIbsCbs / 100m,
        AliqExtintosServDesp    = req.AliqExtintosServDesp / 100m,
        AliqExtitosCmv          = req.AliqExtitosCmv / 100m,
        LR_AliqExtintosReceita  = req.LR_AliqExtintosReceita / 100m,
        LR_AliqIrpjCsll         = req.LR_AliqIrpjCsll / 100m,
        LP_AliqExtintosReceita  = req.LP_AliqExtintosReceita / 100m,
        LP_PctPresuncaoProd     = req.LP_PctPresuncaoProd / 100m,
        LP_PctPresuncaoServ     = req.LP_PctPresuncaoServ / 100m,
        LP_AliqIrpj             = req.LP_AliqIrpj / 100m,
        LP_AliqCsll             = req.LP_AliqCsll / 100m,
        SN_AliqExtintosReceita  = req.SN_AliqExtintosReceita / 100m,
        SN_AliqIrpjDas          = req.SN_AliqIrpjDas / 100m,
        SN_AliqCsllDas          = req.SN_AliqCsllDas / 100m,
        SN_AliqCppDas           = req.SN_AliqCppDas / 100m,
        SNS_AliqExtintosReceita = req.SNS_AliqExtintosReceita / 100m,
        SNS_AliqIbsCbsDas       = req.SNS_AliqIbsCbsDas / 100m,
        SNS_AliqIbsCbsCompras   = req.SNS_AliqIbsCbsCompras / 100m,
        SNS_AliqIrpjDas         = req.SNS_AliqIrpjDas / 100m,
        SNS_AliqCsllDas         = req.SNS_AliqCsllDas / 100m,
    };

    return Results.Ok(Calculadora.CalcularTodos(e));
});

// ─── Admin ────────────────────────────────────────────────────────────────────

app.MapGet("/api/admin/usuarios", (HttpContext ctx) => {
    var (_, isAdmin) = ValidarToken(ctx);
    if (!isAdmin) return Results.Unauthorized();
    return Results.Ok(CarregarUsuarios().Select(u => new {
        u.Id, u.Nome, u.Email, u.IsAdmin, u.Ativo,
        CriadoEm      = u.CriadoEm == default ? (DateTime?)null : u.CriadoEm,
        UltimoAcesso  = u.UltimoAcesso
    }));
});

app.MapPut("/api/admin/usuarios/{id:int}/toggle", (HttpContext ctx, int id) => {
    var (_, isAdmin) = ValidarToken(ctx);
    if (!isAdmin) return Results.Unauthorized();
    var users = CarregarUsuarios();
    var u = users.FirstOrDefault(u => u.Id == id);
    if (u is null) return Results.NotFound(new { erro = "Usuário não encontrado." });
    if (u.IsAdmin) return Results.BadRequest(new { erro = "Não é possível bloquear a conta admin." });
    u.Ativo = !u.Ativo;
    SalvarUsuarios(users);
    return Results.Ok(new { ativo = u.Ativo, mensagem = u.Ativo ? "Usuário liberado." : "Usuário bloqueado." });
});

app.MapDelete("/api/admin/usuarios/{id:int}", (HttpContext ctx, int id) => {
    var (_, isAdmin) = ValidarToken(ctx);
    if (!isAdmin) return Results.Unauthorized();
    var users = CarregarUsuarios();
    var u = users.FirstOrDefault(u => u.Id == id);
    if (u is null) return Results.NotFound(new { erro = "Usuário não encontrado." });
    if (u.IsAdmin) return Results.BadRequest(new { erro = "Não é possível excluir a conta admin." });
    users.Remove(u);
    SalvarUsuarios(users);
    return Results.Ok(new { mensagem = "Usuário excluído." });
});

app.MapPut("/api/admin/minha-conta", async (HttpContext ctx) => {
    var (email, _) = ValidarToken(ctx);
    if (email is null) return Results.Unauthorized();

    var req = await JsonSerializer.DeserializeAsync<ContaReq>(ctx.Request.Body, FileJson);
    if (req is null || string.IsNullOrEmpty(req.SenhaAtual))
        return Results.BadRequest(new { erro = "Informe a senha atual." });

    var users = CarregarUsuarios();
    var u = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    if (u is null) return Results.Unauthorized();

    if (!VerificarSenha(req.SenhaAtual, u.SenhaHash))
        return Results.BadRequest(new { erro = "Senha atual incorreta." });

    if (!string.IsNullOrEmpty(req.NovoEmail)) {
        if (users.Any(x => x.Id != u.Id && x.Email.Equals(req.NovoEmail, StringComparison.OrdinalIgnoreCase)))
            return Results.BadRequest(new { erro = "Este email já está em uso." });
        u.Email = req.NovoEmail;
    }

    if (!string.IsNullOrEmpty(req.NovaSenha)) {
        if (req.NovaSenha.Length < 6) return Results.BadRequest(new { erro = "Senha mínima: 6 caracteres." });
        u.SenhaHash = HashSenha(req.NovaSenha);
    }

    SalvarUsuarios(users);
    return Results.Ok(new { mensagem = "Dados atualizados. Faça login novamente." });
});

app.Run();

// ─── Models ──────────────────────────────────────────────────────────────────

record LoginReq(string Email, string Senha);
record RegistroReq(string Nome, string Email, string Senha);
record ContaReq(string SenhaAtual, string? NovoEmail, string? NovaSenha);

class Usuario {
    public int       Id           { get; set; }
    public string    Nome         { get; set; } = "";
    public string    Email        { get; set; } = "";
    public string    SenhaHash    { get; set; } = "";
    public bool      IsAdmin      { get; set; }
    public bool      Ativo        { get; set; }
    public DateTime  CriadoEm    { get; set; }
    public DateTime? UltimoAcesso { get; set; }
}

class EmpresaReq {
    public decimal FatProduto   { get; set; }
    public decimal FatServico   { get; set; }
    [JsonPropertyName("pctCMV")] public decimal PctCMV { get; set; }
    [JsonPropertyName("pctCSV")] public decimal PctCSV { get; set; }
    public decimal PctDespesas   { get; set; }
    public decimal PctDirCredito { get; set; }
    public decimal AliqIbsCbs            { get; set; }
    public decimal AliqExtintosServDesp  { get; set; }
    public decimal AliqExtitosCmv        { get; set; }
    [JsonPropertyName("LR_AliqExtintosReceita")] public decimal LR_AliqExtintosReceita { get; set; }
    [JsonPropertyName("LR_AliqIrpjCsll")]        public decimal LR_AliqIrpjCsll        { get; set; }
    [JsonPropertyName("LP_AliqExtintosReceita")] public decimal LP_AliqExtintosReceita { get; set; }
    [JsonPropertyName("LP_PctPresuncaoProd")]     public decimal LP_PctPresuncaoProd    { get; set; }
    [JsonPropertyName("LP_PctPresuncaoServ")]     public decimal LP_PctPresuncaoServ    { get; set; }
    [JsonPropertyName("LP_AliqIrpj")]            public decimal LP_AliqIrpj            { get; set; }
    [JsonPropertyName("LP_AliqCsll")]            public decimal LP_AliqCsll            { get; set; }
    [JsonPropertyName("SN_AliqExtintosReceita")] public decimal SN_AliqExtintosReceita { get; set; }
    [JsonPropertyName("SN_AliqIrpjDas")]         public decimal SN_AliqIrpjDas         { get; set; }
    [JsonPropertyName("SN_AliqCsllDas")]         public decimal SN_AliqCsllDas         { get; set; }
    [JsonPropertyName("SN_AliqCppDas")]          public decimal SN_AliqCppDas          { get; set; }
    [JsonPropertyName("SNS_AliqExtintosReceita")]public decimal SNS_AliqExtintosReceita { get; set; }
    [JsonPropertyName("SNS_AliqIbsCbsDas")]      public decimal SNS_AliqIbsCbsDas      { get; set; }
    [JsonPropertyName("SNS_AliqIbsCbsCompras")]  public decimal SNS_AliqIbsCbsCompras  { get; set; }
    [JsonPropertyName("SNS_AliqIrpjDas")]        public decimal SNS_AliqIrpjDas        { get; set; }
    [JsonPropertyName("SNS_AliqCsllDas")]        public decimal SNS_AliqCsllDas        { get; set; }
}

// ─── Domain ──────────────────────────────────────────────────────────────────

public class Empresa {
    public decimal FatProduto { get; set; }
    public decimal FatServico { get; set; }
    public decimal FatTotal   => FatProduto + FatServico;
    public decimal PctCMV        { get; set; }
    public decimal PctCSV        { get; set; }
    public decimal PctDespesas   { get; set; }
    public decimal PctDirCredito { get; set; }
    public decimal AliqIbsCbs           { get; set; }
    public decimal AliqExtintosServDesp { get; set; }
    public decimal AliqExtitosCmv       { get; set; }
    public decimal LR_AliqExtintosReceita { get; set; }
    public decimal LR_AliqIrpjCsll        { get; set; }
    public decimal LP_AliqExtintosReceita { get; set; }
    public decimal LP_PctPresuncaoProd    { get; set; }
    public decimal LP_PctPresuncaoServ    { get; set; }
    public decimal LP_AliqIrpj            { get; set; }
    public decimal LP_AliqCsll            { get; set; }
    public decimal SN_AliqExtintosReceita { get; set; }
    public decimal SN_AliqIrpjDas         { get; set; }
    public decimal SN_AliqCsllDas         { get; set; }
    public decimal SN_AliqCppDas          { get; set; }
    public decimal SNS_AliqExtintosReceita { get; set; }
    public decimal SNS_AliqIbsCbsDas       { get; set; }
    public decimal SNS_AliqIbsCbsCompras   { get; set; }
    public decimal SNS_AliqIrpjDas         { get; set; }
    public decimal SNS_AliqCsllDas         { get; set; }
}

public class DRELinhas {
    public decimal ReceitaBruta { get; set; }
    public decimal Deducoes     { get; set; }
    public decimal CmvCsv       { get; set; }
    public decimal Despesas     { get; set; }
    public decimal CppDas       { get; set; }
    public decimal IrpjCsll     { get; set; }
    public decimal ReceitaLiquida     => ReceitaBruta + Deducoes;
    public decimal MargemContribuicao => ReceitaLiquida + CmvCsv;
    public decimal LucroAntesIR       => MargemContribuicao + Despesas + CppDas;
    public decimal LucroLiquido       => LucroAntesIR + IrpjCsll;
}

public class ResultadoDRE {
    public string    Regime    { get; set; } = "";
    public bool      IsSimples { get; set; }
    public DRELinhas Antes     { get; set; } = new();
    public DRELinhas Depois    { get; set; } = new();
}

public static class Calculadora {
    public static List<ResultadoDRE> CalcularTodos(Empresa e) =>
    [
        CalcularLucroReal(e),
        CalcularLucroPresumido(e),
        CalcularSimplesComCredito(e),
        CalcularSimplesSemCredito(e),
    ];

    static ResultadoDRE CalcularLucroReal(Empresa e) {
        var antes = new DRELinhas {
            ReceitaBruta = e.FatTotal,
            Deducoes     = -e.FatTotal * e.LR_AliqExtintosReceita,
            CmvCsv       = -CmvCsvAntes(e),
            Despesas     = -e.FatTotal * e.PctDespesas,
        };
        antes.IrpjCsll = IrpjSobreLucro(antes.LucroAntesIR, e.LR_AliqIrpjCsll);
        var depois = new DRELinhas {
            ReceitaBruta = e.FatTotal * (1 - e.LR_AliqExtintosReceita),
            CmvCsv       = -(CmvDepois(e) + CsvDepoisPadrao(e)),
            Despesas     = -DespDepois(e),
        };
        depois.IrpjCsll = IrpjSobreLucro(depois.LucroAntesIR, e.LR_AliqIrpjCsll);
        return new ResultadoDRE { Regime = "Lucro Real", Antes = antes, Depois = depois };
    }

    static ResultadoDRE CalcularLucroPresumido(Empresa e) {
        decimal baseIr   = e.FatProduto * e.LP_PctPresuncaoProd + e.FatServico * e.LP_PctPresuncaoServ;
        decimal irpjCsll = -(baseIr * (e.LP_AliqIrpj + e.LP_AliqCsll));
        var antes = new DRELinhas {
            ReceitaBruta = e.FatTotal,
            Deducoes     = -e.FatTotal * e.LP_AliqExtintosReceita,
            CmvCsv       = -CmvCsvAntes(e),
            Despesas     = -e.FatTotal * e.PctDespesas,
            IrpjCsll     = irpjCsll,
        };
        var depois = new DRELinhas {
            ReceitaBruta = e.FatTotal * (1 - e.LP_AliqExtintosReceita),
            CmvCsv       = -(CmvDepois(e) + CsvDepoisPadrao(e)),
            Despesas     = -DespDepois(e),
            IrpjCsll     = irpjCsll,
        };
        return new ResultadoDRE { Regime = "Lucro Presumido", Antes = antes, Depois = depois };
    }

    static ResultadoDRE CalcularSimplesComCredito(Empresa e) {
        decimal irpjCsll = -e.FatTotal * (e.SN_AliqIrpjDas + e.SN_AliqCsllDas);
        decimal cpp      = -e.FatTotal * e.SN_AliqCppDas;
        var antes = new DRELinhas {
            ReceitaBruta = e.FatTotal,
            Deducoes     = -e.FatTotal * e.SN_AliqExtintosReceita,
            CmvCsv       = -CmvCsvAntes(e),
            Despesas     = -e.FatTotal * e.PctDespesas,
            CppDas       = cpp, IrpjCsll = irpjCsll,
        };
        var depois = new DRELinhas {
            ReceitaBruta = e.FatTotal * (1 - e.SN_AliqExtintosReceita),
            CmvCsv       = -(CmvDepois(e) + CsvDepoisSimples(e)),
            Despesas     = -DespDepois(e),
            CppDas       = cpp, IrpjCsll = irpjCsll,
        };
        return new ResultadoDRE { Regime = "Simples c/ Crédito IBS", IsSimples = true, Antes = antes, Depois = depois };
    }

    static ResultadoDRE CalcularSimplesSemCredito(Empresa e) {
        decimal irpjCsll = -e.FatTotal * (e.SNS_AliqIrpjDas + e.SNS_AliqCsllDas);
        decimal cpp      = -e.FatTotal * e.SN_AliqCppDas;
        var antes = new DRELinhas {
            ReceitaBruta = e.FatTotal,
            Deducoes     = -e.FatTotal * e.SNS_AliqExtintosReceita,
            CmvCsv       = -CmvCsvAntes(e),
            Despesas     = -e.FatTotal * e.PctDespesas,
            CppDas       = cpp, IrpjCsll = irpjCsll,
        };
        decimal fIbs    = 1 + e.SNS_AliqIbsCbsCompras;
        decimal cmvSNS  = e.FatProduto * e.PctCMV * (1 - e.AliqExtitosCmv) * fIbs;
        decimal csvSNS  = e.FatServico * e.PctCSV * (1 - e.PctDirCredito * e.PctDirCredito * e.AliqExtintosServDesp) * fIbs;
        decimal despSNS = e.FatTotal   * e.PctDespesas * (1 - e.PctDirCredito * e.AliqExtintosServDesp) * fIbs;
        var depois = new DRELinhas {
            ReceitaBruta = e.FatTotal,
            Deducoes     = -e.FatTotal * e.SNS_AliqIbsCbsDas,
            CmvCsv       = -(cmvSNS + csvSNS),
            Despesas     = -despSNS,
            CppDas       = cpp, IrpjCsll = irpjCsll,
        };
        return new ResultadoDRE { Regime = "Simples s/ Crédito IBS", IsSimples = true, Antes = antes, Depois = depois };
    }

    static decimal CmvCsvAntes(Empresa e)     => e.FatProduto * e.PctCMV + e.FatServico * e.PctCSV;
    static decimal CmvDepois(Empresa e)        => e.FatProduto * e.PctCMV * (1 - e.AliqExtitosCmv);
    static decimal CsvDepoisPadrao(Empresa e)  => e.FatServico * e.PctCSV * (1 - e.PctDirCredito * e.AliqExtintosServDesp);
    static decimal CsvDepoisSimples(Empresa e) => e.FatServico * e.PctCSV * (1 - e.PctDirCredito * e.PctDirCredito * e.AliqExtintosServDesp);
    static decimal DespDepois(Empresa e)       => e.FatTotal   * e.PctDespesas * (1 - e.PctDirCredito * e.AliqExtintosServDesp);
    static decimal IrpjSobreLucro(decimal lucro, decimal aliq) => lucro > 0 ? -lucro * aliq : 0;
}
