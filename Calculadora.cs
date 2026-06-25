namespace CalculoContabilidade;

public static class Calculadora
{
    public static List<ResultadoDRE> CalcularTodos(Empresa e) =>
    [
        CalcularLucroReal(e),
        CalcularLucroPresumido(e),
        CalcularSimplesComCredito(e),
        CalcularSimplesSemCredito(e),
    ];

    // ── Lucro Real ──────────────────────────────────────────────────────────

    public static ResultadoDRE CalcularLucroReal(Empresa e)
    {
        var antes = new DRELinhas
        {
            ReceitaBruta = e.FatTotal,
            Deducoes     = -e.FatTotal * e.LR_AliqExtintosReceita,
            CmvCsv       = -CmvCsvAntes(e),
            Despesas     = -e.FatTotal * e.PctDespesas,
        };
        antes.IrpjCsll = IrpjCsllSobreLucro(antes.LucroAntesIR, e.LR_AliqIrpjCsll);

        var depois = new DRELinhas
        {
            ReceitaBruta = e.FatTotal * (1 - e.LR_AliqExtintosReceita),
            Deducoes     = 0,
            CmvCsv       = -(CmvDepois(e) + CsvDepoisPadrao(e)),
            Despesas     = -DespDepois(e),
        };
        depois.IrpjCsll = IrpjCsllSobreLucro(depois.LucroAntesIR, e.LR_AliqIrpjCsll);

        return new ResultadoDRE { Regime = "Lucro Real", Antes = antes, Depois = depois };
    }

    // ── Lucro Presumido ─────────────────────────────────────────────────────

    public static ResultadoDRE CalcularLucroPresumido(Empresa e)
    {
        // Base para IRPJ/CSLL no Presumido não muda com a reforma
        // (é calculada sobre a receita bruta total, que inclui IBS/CBS cobrado do cliente)
        decimal baseIr = e.FatProduto * e.LP_PctPresuncaoProd + e.FatServico * e.LP_PctPresuncaoServ;
        decimal irpjCsllFixo = -(baseIr * (e.LP_AliqIrpj + e.LP_AliqCsll));

        var antes = new DRELinhas
        {
            ReceitaBruta = e.FatTotal,
            Deducoes     = -e.FatTotal * e.LP_AliqExtintosReceita,
            CmvCsv       = -CmvCsvAntes(e),
            Despesas     = -e.FatTotal * e.PctDespesas,
            IrpjCsll     = irpjCsllFixo,
        };

        var depois = new DRELinhas
        {
            ReceitaBruta = e.FatTotal * (1 - e.LP_AliqExtintosReceita),
            Deducoes     = 0,
            CmvCsv       = -(CmvDepois(e) + CsvDepoisPadrao(e)),
            Despesas     = -DespDepois(e),
            IrpjCsll     = irpjCsllFixo, // base não muda (sobre receita total incl. IBS/CBS)
        };

        return new ResultadoDRE { Regime = "Lucro Presumido", Antes = antes, Depois = depois };
    }

    // ── Simples Nacional COM crédito IBS/CBS ────────────────────────────────
    //
    // Após a reforma: ISS/PIS/COFINS saem do DAS; IBS/CBS cobrado separado (fora do DAS).
    // O IBS/CBS não compõe a DRE (é crédito/débito separado).
    // Os custos caem porque fornecedores também removem os impostos extintos dos preços.
    // Para CSV, o fator é DirCredito² pois o prestador de serviços (também em Simples)
    // tem o mesmo percentual de mão-de-obra excluído em seus próprios custos.

    public static ResultadoDRE CalcularSimplesComCredito(Empresa e)
    {
        decimal irpjCsll = -e.FatTotal * (e.SN_AliqIrpjDas + e.SN_AliqCsllDas);
        decimal cpp      = -e.FatTotal * e.SN_AliqCppDas;

        var antes = new DRELinhas
        {
            ReceitaBruta = e.FatTotal,
            Deducoes     = -e.FatTotal * e.SN_AliqExtintosReceita,
            CmvCsv       = -CmvCsvAntes(e),
            Despesas     = -e.FatTotal * e.PctDespesas,
            CppDas       = cpp,
            IrpjCsll     = irpjCsll,
        };

        var depois = new DRELinhas
        {
            ReceitaBruta = e.FatTotal * (1 - e.SN_AliqExtintosReceita),
            Deducoes     = 0,
            CmvCsv       = -(CmvDepois(e) + CsvDepoisSimples(e)),
            Despesas     = -DespDepois(e),
            CppDas       = cpp,
            IrpjCsll     = irpjCsll,
        };

        return new ResultadoDRE { Regime = "Simples c/ Crédito IBS", IsSimples = true, Antes = antes, Depois = depois };
    }

    // ── Simples Nacional SEM crédito IBS/CBS ────────────────────────────────
    //
    // O IBS/CBS permanece DENTRO do DAS (a uma alíquota menor pela tabela).
    // Porém, os fornecedores cobram IBS/CBS e a empresa não pode se creditar.
    // Resultado: custos aumentam (extintos saem, mas IBS/CBS entra no custo sem crédito).

    public static ResultadoDRE CalcularSimplesSemCredito(Empresa e)
    {
        decimal irpjCsll = -e.FatTotal * (e.SNS_AliqIrpjDas + e.SNS_AliqCsllDas);
        decimal cpp      = -e.FatTotal * e.SN_AliqCppDas;

        var antes = new DRELinhas
        {
            ReceitaBruta = e.FatTotal,
            Deducoes     = -e.FatTotal * e.SNS_AliqExtintosReceita,
            CmvCsv       = -CmvCsvAntes(e),
            Despesas     = -e.FatTotal * e.PctDespesas,
            CppDas       = cpp,
            IrpjCsll     = irpjCsll,
        };

        // Custo base (após remoção dos impostos extintos) é o mesmo que Simples c/ crédito,
        // mas o IBS/CBS das compras NÃO gera crédito → entra integralmente no custo.
        decimal fIbs = 1 + e.SNS_AliqIbsCbsCompras;
        decimal cmvSNS  = e.FatProduto * e.PctCMV * (1 - e.AliqExtitosCmv) * fIbs;
        decimal csvSNS  = e.FatServico * e.PctCSV * (1 - e.PctDirCredito * e.PctDirCredito * e.AliqExtintosServDesp) * fIbs;
        decimal despSNS = e.FatTotal   * e.PctDespesas * (1 - e.PctDirCredito * e.AliqExtintosServDesp) * fIbs;

        var depois = new DRELinhas
        {
            ReceitaBruta = e.FatTotal,
            Deducoes     = -e.FatTotal * e.SNS_AliqIbsCbsDas, // nova alíquota IBS/CBS no DAS
            CmvCsv       = -(cmvSNS + csvSNS),
            Despesas     = -despSNS,
            CppDas       = cpp,
            IrpjCsll     = irpjCsll,
        };

        return new ResultadoDRE { Regime = "Simples s/ Crédito IBS", IsSimples = true, Antes = antes, Depois = depois };
    }

    // ── Helpers internos ────────────────────────────────────────────────────

    private static decimal CmvCsvAntes(Empresa e) =>
        e.FatProduto * e.PctCMV + e.FatServico * e.PctCSV;

    // CMV produtos: fornecedor remove ICMS/IPI/PIS/COFINS do preço
    private static decimal CmvDepois(Empresa e) =>
        e.FatProduto * e.PctCMV * (1 - e.AliqExtitosCmv);

    // CSV (LR/LP): redução proporcional ao % c/ direito a crédito
    private static decimal CsvDepoisPadrao(Empresa e) =>
        e.FatServico * e.PctCSV * (1 - e.PctDirCredito * e.AliqExtintosServDesp);

    // CSV Simples: prestador de serviço (também em Simples) tem mesmo % de mão-de-obra
    // → fator DirCredito ao quadrado
    private static decimal CsvDepoisSimples(Empresa e) =>
        e.FatServico * e.PctCSV * (1 - e.PctDirCredito * e.PctDirCredito * e.AliqExtintosServDesp);

    // Despesas (igual para todos os regimes c/ crédito)
    private static decimal DespDepois(Empresa e) =>
        e.FatTotal * e.PctDespesas * (1 - e.PctDirCredito * e.AliqExtintosServDesp);

    // IRPJ/CSLL sobre lucro (Lucro Real): zero se prejuízo
    private static decimal IrpjCsllSobreLucro(decimal lucroAntesIR, decimal aliq) =>
        lucroAntesIR > 0 ? -lucroAntesIR * aliq : 0;
}
