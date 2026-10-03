using Xunit;

namespace Conde.Sufixo.Testes;

/// <summary>
/// O Manacher, contra a forca bruta que testa cada faixa.
/// </summary>
public class PalindromoTest
{
    /// <summary>
    /// O maior palindromo, em TODAS as cadeias de ate 12 caracteres sobre dois
    /// simbolos.
    ///
    /// O erro que o Manacher esconde e usar o valor do espelho sem corta-lo no
    /// que sobra da borda, e ele so aparece em cadeias com estrutura. Aqui elas
    /// sao todas as cadeias.
    /// </summary>
    [Fact]
    public void OMaiorBateComAForcaBrutaEmTodasAsCadeiasDeAte12()
    {
        for (var tamanho = 1; tamanho <= 12; tamanho++)
            foreach (var texto in Juiz.TodasAsCadeias(tamanho, "ab"))
            {
                var pelaForca = Palindromo.MaiorPorForcaBruta(texto);
                var peloManacher = Palindromo.Maior(texto);
                Assert.Equal(pelaForca.Length, peloManacher.Length);
                // E o que ele devolveu e de fato um palindromo do texto.
                Assert.Equal(peloManacher, new string(peloManacher.Reverse().ToArray()));
                Assert.Contains(peloManacher, texto, StringComparison.Ordinal);
            }
    }

    /// <summary>
    /// A CONTAGEM de palindromos, que e a medida mais sensivel: ela so fecha se
    /// cada raio estiver exatamente certo, e nao apenas o maior deles.
    /// </summary>
    [Fact]
    public void AContagemBateComAForcaBrutaEmTodasAsCadeiasDeAte11()
    {
        for (var tamanho = 1; tamanho <= 11; tamanho++)
            foreach (var texto in Juiz.TodasAsCadeias(tamanho, "ab"))
                Assert.Equal(Palindromo.QuantosPorForcaBruta(texto), Palindromo.Quantos(texto));
    }

    /// <summary>E com tres simbolos, onde os palindromos sao mais raros.</summary>
    [Fact]
    public void AContagemBateComTresSimbolos()
    {
        for (var tamanho = 1; tamanho <= 8; tamanho++)
            foreach (var texto in Juiz.TodasAsCadeias(tamanho, "abc"))
            {
                Assert.Equal(Palindromo.QuantosPorForcaBruta(texto), Palindromo.Quantos(texto));
                Assert.Equal(Palindromo.MaiorPorForcaBruta(texto).Length, Palindromo.Maior(texto).Length);
            }
    }

    /// <summary>
    /// A pergunta em TEMPO CONSTANTE: depois da passada linear, qualquer faixa
    /// e respondida com uma comparacao. O teste confere TODAS as faixas.
    /// </summary>
    [Fact]
    public void APerguntaPorFaixaBateComAVerificacaoDireta()
    {
        foreach (var texto in Juiz.TodasAsCadeias(10, "ab"))
        {
            var raios = Palindromo.Raios(texto);
            for (var inicio = 0; inicio <= texto.Length; inicio++)
                for (var fim = inicio; fim <= texto.Length; fim++)
                {
                    var pedaco = texto[inicio..fim];
                    var esperado = pedaco == new string(pedaco.Reverse().ToArray());
                    Assert.Equal(esperado, Palindromo.EhPalindromo(raios, inicio, fim));
                }
        }
    }

    /// <summary>
    /// Os casos conhecidos, conferidos na mao, incluindo palindromos de tamanho
    /// PAR, que sao os que o truque da intercalacao existe para resolver.
    /// </summary>
    [Fact]
    public void OsCasosConhecidos()
    {
        Assert.Equal("aba", Palindromo.Maior("xabay"));
        Assert.Equal("abba", Palindromo.Maior("xabbay"));
        Assert.Equal("aaaa", Palindromo.Maior("aaaa"));
        Assert.Equal(1, Palindromo.Maior("abc").Length);
        Assert.Equal(string.Empty, Palindromo.Maior(string.Empty));

        // "aaaa" tem 10 palindromos: 4 de um, 3 de dois, 2 de tres, 1 de quatro.
        Assert.Equal(10, Palindromo.Quantos("aaaa"));
        // "abc" tem 3: so os caracteres.
        Assert.Equal(3, Palindromo.Quantos("abc"));
        // E "abba" tem 6: a, b, b, a, bb, abba.
        Assert.Equal(6, Palindromo.Quantos("abba"));
    }

    /// <summary>
    /// O texto todo igual e o pior caso da forca bruta e o melhor teste do
    /// reaproveitamento: todas as n(n+1)/2 faixas sao palindromos.
    /// </summary>
    [Fact]
    public void OTextoTodoIgualTemTodasAsFaixasPalindromicas()
    {
        foreach (var n in new[] { 1, 10, 100, 5000 })
        {
            var texto = new string('a', n);
            Assert.Equal((long)n * (n + 1) / 2, Palindromo.Quantos(texto));
            Assert.Equal(n, Palindromo.Maior(texto).Length);
        }
    }

    /// <summary>
    /// A intercalacao poe o separador em 2n+1 posicoes, e o raio no texto
    /// intercalado E o tamanho do palindromo no original.
    /// </summary>
    [Fact]
    public void AIntercalacaoTemODobroMaisUmDePosicoes()
    {
        foreach (var texto in new[] { "", "a", "ab", "aba" })
        {
            var intercalado = Palindromo.Intercalar(texto);
            Assert.Equal(2 * texto.Length + 1, intercalado.Length);
            Assert.Equal(texto.Length + 1, intercalado.Count(c => c == Palindromo.Separador));
            Assert.Equal(texto, new string(intercalado.Where(c => c != Palindromo.Separador).ToArray()));
        }

        // E o raio do centro de "aba" vale 3, que e o tamanho do palindromo.
        var raios = Palindromo.Raios("aba");
        Assert.Equal(3, raios.Max());
    }

    [Fact]
    public void EntradaInvalidaEhRecusada()
    {
        Assert.Throws<ArgumentException>(() => Palindromo.Raios("a" + Palindromo.Separador + "b"));
        var raios = Palindromo.Raios("abc");
        Assert.Throws<ArgumentOutOfRangeException>(() => Palindromo.EhPalindromo(raios, -1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => Palindromo.EhPalindromo(raios, 0, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => Palindromo.EhPalindromo(raios, 2, 1));
        // Faixa vazia e de um caractere sao palindromos por definicao.
        Assert.True(Palindromo.EhPalindromo(raios, 1, 1));
        Assert.True(Palindromo.EhPalindromo(raios, 1, 2));
    }
}
