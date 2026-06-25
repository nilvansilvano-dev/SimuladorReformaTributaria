using System.Globalization;

namespace CalculoContabilidade;

class Program
{
    static readonly CultureInfo PtBr = new("pt-BR");

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        bool continuar = true;
        while (continuar)
        {
            TryLimpar();
            Banner();

            var empresa   = ColetarDados();
            var resultados = Calculadora.CalcularTodos(empresa);

            TryLimpar();
            Banner();

            foreach (var r in resultados)
                ExibirDRE(r);

            ExibirComparativo(resultados);

            Console.WriteLine();
            Console.Write("  Calcular novamente? (S/N): ");
            continuar = (Console.ReadLine() ?? "").Trim().Equals("S", StringComparison.OrdinalIgnoreCase);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Coleta de dados
    // ═══════════════════════════════════════════════════════════════════════

    static Empresa ColetarDados()
    {
        var e = new Empresa();

        Titulo("DADOS DA EMPRESA");
        e.FatProduto = LerMoeda("Faturamento Produtos (R$)", 0);
        e.FatServico = LerMoeda("Faturamento Serviços (R$)", 50_000);
        Console.WriteLine($"\n  Faturamento Total: {M(e.FatTotal)}\n");

        e.PctCMV        = LerPct("% CMV - Custo de Mercadorias/Produtos", 10);
        e.PctCSV        = LerPct("% CSV - Custo do Serviço Prestado", 70);
        e.PctDespesas   = LerPct("% Despesas Operacionais", 5);
        e.PctDirCredito = LerPct("% Desp/Serv com direito a crédito IBS/CBS (excl. folha)", 70);

        Titulo("PARÂMETROS DA REFORMA");
        e.AliqIbsCbs            = LerPct("Alíquota IBS/CBS", 28);
        e.AliqExtintosServDesp  = LerPct("% Extintos em Desp/Serviços (ISS+PIS+COFINS)", 20.65m);
        e.AliqExtitosCmv        = LerPct("% Extintos em CMV Produtos (ICMS+IPI+PIS+COFINS)", 24.15m);

        Titulo("LUCRO REAL");
        e.LR_AliqExtintosReceita = LerPct("% Extintos na Receita (ICMS+ISS+PIS+COFINS+IPI)", 6.78m);
        e.LR_AliqIrpjCsll        = LerPct("% IRPJ + CSLL sobre o Lucro", 24);

        Titulo("LUCRO PRESUMIDO");
        e.LP_AliqExtintosReceita = LerPct("% Extintos na Receita", 6.56m);
        e.LP_PctPresuncaoProd    = LerPct("% Presunção IRPJ - Produtos/Comércio", 8);
        e.LP_PctPresuncaoServ    = LerPct("% Presunção IRPJ - Serviços", 32);
        e.LP_AliqIrpj            = LerPct("Alíquota IRPJ", 15);
        e.LP_AliqCsll            = LerPct("Alíquota CSLL", 9);

        Titulo("SIMPLES NACIONAL (COM crédito IBS/CBS)");
        e.SN_AliqExtintosReceita = LerPct("% ISS+PIS+COFINS no DAS (extintos pós-reforma)", 6.89m);
        e.SN_AliqIrpjDas         = LerPct("% IRPJ no DAS", 0.56m);
        e.SN_AliqCsllDas         = LerPct("% CSLL no DAS", 0.49m);
        e.SN_AliqCppDas          = LerPct("% CPP-INSS no DAS", 6.08m);

        Titulo("SIMPLES NACIONAL (SEM crédito IBS/CBS)");
        e.SNS_AliqExtintosReceita = LerPct("% ISS+PIS+COFINS no DAS (antes da reforma)", 6.888m);
        e.SNS_AliqIbsCbsDas       = LerPct("% IBS/CBS dentro do DAS após reforma (tabela faixa)", 6.5437m);
        e.SNS_AliqIbsCbsCompras   = LerPct("% IBS/CBS nas compras sem crédito (efetivo)", 26.5m);
        e.SNS_AliqIrpjDas         = LerPct("% IRPJ no DAS", 0.561m);
        e.SNS_AliqCsllDas         = LerPct("% CSLL no DAS", 0.491m);

        return e;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Exibição de resultados
    // ═══════════════════════════════════════════════════════════════════════

    static void ExibirDRE(ResultadoDRE r)
    {
        Console.WriteLine();
        Cor(ConsoleColor.Yellow);
        Console.WriteLine($"  ══ {r.Regime.ToUpper()} " + new string('═', Math.Max(0, 50 - r.Regime.Length)));
        Reset();

        CabecalhoDRE();
        Sep();

        LinhaM("Receita Bruta",                   r.Antes.ReceitaBruta,        r.Depois.ReceitaBruta);
        LinhaM(r.IsSimples ? "(-) DAS (ISS/PIS/COFINS → IBS/CBS)" : "(-) Deduções (ICMS/ISS/PIS/COFINS)",
                                                   r.Antes.Deducoes,            r.Depois.Deducoes);
        LinhaM("Receita Líquida",                  r.Antes.ReceitaLiquida,      r.Depois.ReceitaLiquida,   bold: true);
        Sep();
        LinhaM("(-) CMV + CSV",                    r.Antes.CmvCsv,             r.Depois.CmvCsv);
        LinhaM("Margem de Contribuição",            r.Antes.MargemContribuicao, r.Depois.MargemContribuicao, bold: true);
        Sep();
        LinhaM(r.IsSimples ? "(-) Despesas + CPP-INSS" : "(-) Despesas",
                                                   r.Antes.Despesas + r.Antes.CppDas,
                                                   r.Depois.Despesas + r.Depois.CppDas);
        LinhaM("Lucro Antes do IRPJ/CSLL",         r.Antes.LucroAntesIR,       r.Depois.LucroAntesIR,     bold: true);
        Sep();
        LinhaM("(-) IRPJ e CSLL",                  r.Antes.IrpjCsll,           r.Depois.IrpjCsll);
        Sep();
        LinhaM("LUCRO LÍQUIDO",                    r.Antes.LucroLiquido,        r.Depois.LucroLiquido,     bold: true, lucro: true);
    }

    static void ExibirComparativo(List<ResultadoDRE> resultados)
    {
        Console.WriteLine();
        Cor(ConsoleColor.Cyan);
        Console.WriteLine("  ══ COMPARATIVO ENTRE REGIMES " + new string('═', 35));
        Reset();
        Console.WriteLine();
        Console.WriteLine($"  {"REGIME",-28} {"ANTES",14}  {"DEPOIS",14}  {"VARIAÇÃO",14}  {"% VAR",8}");
        Console.WriteLine("  " + new string('─', 82));

        foreach (var r in resultados)
        {
            var varStr = (r.VariacaoLucro >= 0 ? "+" : "") + M(r.VariacaoLucro);
            var pctStr = (r.PctVariacao >= 0 ? "+" : "") + r.PctVariacao.ToString("P2", PtBr);

            Console.Write($"  {r.Regime,-28} {M(r.Antes.LucroLiquido),14}  {M(r.Depois.LucroLiquido),14}  ");
            Cor(r.VariacaoLucro >= 0 ? ConsoleColor.Green : ConsoleColor.Red);
            Console.Write($"{varStr,14}  {pctStr,8}");
            Reset();
            Console.WriteLine();
        }

        Console.WriteLine("  " + new string('─', 82));
        Console.WriteLine();
        Cor(ConsoleColor.DarkGray);
        Console.WriteLine("  * Adicional IRPJ (10% s/ presunção > R$240k/ano) não calculado nesta versão.");
        Console.WriteLine("  * IBS/CBS não compõe a DRE nos regimes com crédito (débito/crédito separado).");
        Reset();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helpers de display
    // ═══════════════════════════════════════════════════════════════════════

    static void Banner()
    {
        Cor(ConsoleColor.Cyan);
        Console.WriteLine("  ╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("  ║    SIMULADOR DE IMPACTO DA REFORMA TRIBUTÁRIA - IBS/CBS      ║");
        Console.WriteLine("  ║                www.reformatributaria.inf.br                  ║");
        Console.WriteLine("  ╚══════════════════════════════════════════════════════════════╝");
        Reset();
        Console.WriteLine();
    }

    static void Titulo(string texto)
    {
        Console.WriteLine();
        Cor(ConsoleColor.DarkGray);
        Console.WriteLine($"  ── {texto} " + new string('─', Math.Max(0, 55 - texto.Length)));
        Reset();
    }

    static void CabecalhoDRE()
    {
        Cor(ConsoleColor.DarkGray);
        Console.WriteLine($"  {"DESCRIÇÃO",-34} {"ANTES",14}  {"DEPOIS",14}  {"VARIAÇÃO",14}");
        Reset();
    }

    static void Sep()
    {
        Cor(ConsoleColor.DarkGray);
        Console.WriteLine("  " + new string('·', 80));
        Reset();
    }

    static void LinhaM(string desc, decimal antes, decimal depois, bool bold = false, bool lucro = false)
    {
        var var_ = depois - antes;
        var varStr = var_ == 0 ? "—" : (var_ > 0 ? "+" : "") + M(var_);

        if (bold) Cor(ConsoleColor.White);
        Console.Write($"  {desc,-34} {M(antes),14}  {M(depois),14}  ");

        if (lucro)
            Cor(depois >= 0 ? ConsoleColor.Green : ConsoleColor.Red);
        else
            Cor(var_ > 0 ? ConsoleColor.Green : var_ < 0 ? ConsoleColor.Red : ConsoleColor.DarkGray);

        Console.Write($"{varStr,14}");
        Reset();
        Console.WriteLine();
    }

    static string M(decimal v) => v.ToString("C", PtBr);

    static void Cor(ConsoleColor c) => Console.ForegroundColor = c;
    static void Reset() => Console.ResetColor();
    static void TryLimpar() { try { Console.Clear(); } catch { } }

    // ═══════════════════════════════════════════════════════════════════════
    // Helpers de entrada
    // ═══════════════════════════════════════════════════════════════════════

    static decimal LerMoeda(string prompt, decimal padrao)
    {
        Console.Write($"  {prompt} [{M(padrao)}]: ");
        var input = (Console.ReadLine() ?? "").Trim();
        if (input == "") return padrao;

        // Aceita "50000", "50.000" ou "50.000,00"
        input = input.Replace("R$", "").Replace(".", "").Replace(",", ".").Trim();
        return decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : padrao;
    }

    static decimal LerPct(string prompt, decimal padraoEmPct)
    {
        Console.Write($"  {prompt} [{padraoEmPct:0.###}%]: ");
        var input = (Console.ReadLine() ?? "").Trim();
        if (input == "") return padraoEmPct / 100m;

        // Aceita "20,65" ou "20.65"
        input = input.Replace("%", "").Replace(",", ".").Trim();
        return decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v / 100m : padraoEmPct / 100m;
    }
}
