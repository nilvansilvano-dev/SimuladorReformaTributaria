namespace CalculoContabilidade;

public class Empresa
{
    // ── Faturamento ────────────────────────────────────────
    public decimal FatProduto { get; set; }
    public decimal FatServico { get; set; }
    public decimal FatTotal => FatProduto + FatServico;

    // ── Custos e Despesas ──────────────────────────────────
    public decimal PctCMV { get; set; }        // % custo mercadorias/produtos
    public decimal PctCSV { get; set; }        // % custo serviços prestados
    public decimal PctDespesas { get; set; }   // % despesas operacionais
    public decimal PctDirCredito { get; set; } // % desp/serv com dir. a crédito IBS/CBS (excl. folha)

    // ── Parâmetros da Reforma (comuns) ─────────────────────
    public decimal AliqIbsCbs { get; set; }           // alíquota IBS/CBS (28%)
    public decimal AliqExtintosServDesp { get; set; } // % extintos em desp/serviços (ISS+PIS+COFINS)
    public decimal AliqExtitosCmv { get; set; }       // % extintos em CMV produtos (ICMS+IPI+PIS+COFINS)

    // ── Lucro Real ─────────────────────────────────────────
    public decimal LR_AliqExtintosReceita { get; set; } // % extintos na receita
    public decimal LR_AliqIrpjCsll { get; set; }        // % IRPJ+CSLL sobre lucro

    // ── Lucro Presumido ────────────────────────────────────
    public decimal LP_AliqExtintosReceita { get; set; }
    public decimal LP_PctPresuncaoProd { get; set; }  // % presunção IRPJ produtos (8%)
    public decimal LP_PctPresuncaoServ { get; set; }  // % presunção IRPJ serviços (32%)
    public decimal LP_AliqIrpj { get; set; }          // 15%
    public decimal LP_AliqCsll { get; set; }          // 9%

    // ── Simples Nacional COM crédito IBS/CBS ───────────────
    public decimal SN_AliqExtintosReceita { get; set; } // % ISS+PIS+COFINS no DAS (extintos)
    public decimal SN_AliqIrpjDas { get; set; }         // % IRPJ dentro do DAS
    public decimal SN_AliqCsllDas { get; set; }         // % CSLL dentro do DAS
    public decimal SN_AliqCppDas { get; set; }          // % CPP-INSS dentro do DAS

    // ── Simples Nacional SEM crédito IBS/CBS ──────────────
    public decimal SNS_AliqExtintosReceita { get; set; } // % ISS+PIS+COFINS no DAS (antes da reforma)
    public decimal SNS_AliqIbsCbsDas { get; set; }       // % IBS/CBS dentro do DAS após reforma (tabela)
    public decimal SNS_AliqIbsCbsCompras { get; set; }   // % IBS/CBS nas compras (sem crédito)
    public decimal SNS_AliqIrpjDas { get; set; }
    public decimal SNS_AliqCsllDas { get; set; }
}

public class DRELinhas
{
    public decimal ReceitaBruta { get; set; }
    public decimal Deducoes { get; set; }
    public decimal CmvCsv { get; set; }
    public decimal Despesas { get; set; }
    public decimal CppDas { get; set; }  // CPP-INSS (somente Simples)
    public decimal IrpjCsll { get; set; }

    public decimal ReceitaLiquida      => ReceitaBruta + Deducoes;
    public decimal MargemContribuicao  => ReceitaLiquida + CmvCsv;
    public decimal LucroAntesIR        => MargemContribuicao + Despesas + CppDas;
    public decimal LucroLiquido        => LucroAntesIR + IrpjCsll;
}

public class ResultadoDRE
{
    public string Regime { get; set; } = "";
    public bool IsSimples { get; set; }
    public DRELinhas Antes { get; set; } = new();
    public DRELinhas Depois { get; set; } = new();

    public decimal VariacaoLucro => Depois.LucroLiquido - Antes.LucroLiquido;
    public decimal PctVariacao   => Antes.LucroLiquido != 0
        ? VariacaoLucro / Math.Abs(Antes.LucroLiquido)
        : 0;
}
