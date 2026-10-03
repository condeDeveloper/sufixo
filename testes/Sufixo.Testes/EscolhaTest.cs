using Xunit;

namespace Conde.Sufixo.Testes;

/// <summary>
/// As tres estruturas resolvendo os MESMOS problemas, e o que separa uma da
/// outra.
///
/// Elas estao em teste e nao so no medidor porque sao AFIRMACOES: se alguma
/// deixar de valer, o README passa a mentir.
/// </summary>
public class EscolhaTest
{
    /// <summary>
    /// Os tres caminhos acham as mesmas ocorrencias: vetor de sufixos por busca
    /// binaria, funcao Z por concatenacao, e varredura ingenua.
    /// </summary>
    [Fact]
    public void OsTresCaminhosAchamAsMesmasOcorrencias()
    {
        var sorteio = new Random(3);
        for (var tentativa = 0; tentativa < 300; tentativa++)
        {
            var n = sorteio.Next(1, 60);
            var texto = new string(Enumerable.Range(0, n)
                .Select(_ => (char)('a' + sorteio.Next(3))).ToArray());
            var m = sorteio.Next(1, 6);
            var padrao = new string(Enumerable.Range(0, m)
                .Select(_ => (char)('a' + sorteio.Next(3))).ToArray());

            var esperado = Juiz.Ocorrencias(texto, padrao);
            Assert.Equal(esperado, new Vetor(texto).Ocorrencias(padrao));
            Assert.Equal(esperado, Zeta.Ocorrencias(texto, padrao));
            // E o automato responde a pergunta mais fraca, de existencia.
            Assert.Equal(esperado.Count > 0, new Automato(texto).Contem(padrao));
        }
    }

    /// <summary>
    /// As subcadeias distintas, por tres caminhos sem nada em comum: o conjunto
    /// montado de verdade, a formula do vetor de sufixos, e a soma sobre os
    /// estados do automato.
    /// </summary>
    [Fact]
    public void AsSubcadeiasDistintasConcordamPorTresCaminhos()
    {
        var sorteio = new Random(5);
        for (var tentativa = 0; tentativa < 200; tentativa++)
        {
            var n = sorteio.Next(1, 30);
            var texto = new string(Enumerable.Range(0, n)
                .Select(_ => (char)('a' + sorteio.Next(3))).ToArray());

            var esperado = Juiz.SubcadeiasDistintas(texto);
            Assert.Equal(esperado, new Vetor(texto).SubcadeiasDistintas());
            Assert.Equal(esperado, new Automato(texto).SubcadeiasDistintas());
        }
    }

    /// <summary>
    /// O AUTOMATO e o unico que responde "contem" sem custo no tamanho do
    /// texto: a consulta custa o tamanho da CADEIA perguntada.
    ///
    /// O vetor de sufixos precisa de busca binaria, que e log n comparacoes de
    /// ate m caracteres. A diferenca aparece quando se pergunta muito sobre um
    /// texto grande.
    /// </summary>
    [Fact]
    public void OAutomatoRespondeExistenciaSemOlharOTamanhoDoTexto()
    {
        var sorteio = new Random(7);
        var texto = new string(Enumerable.Range(0, 50_000)
            .Select(_ => (char)('a' + sorteio.Next(4))).ToArray());

        var automato = new Automato(texto);
        var vetor = new Vetor(texto);

        // As respostas sao as mesmas.
        for (var i = 0; i < 200; i++)
        {
            var inicio = sorteio.Next(texto.Length - 10);
            var pedaco = texto.Substring(inicio, sorteio.Next(1, 10));
            Assert.True(automato.Contem(pedaco));
            Assert.NotEmpty(vetor.Ocorrencias(pedaco));
        }

        // E o automato cabe nos limites provados, apesar do texto grande.
        var (limiteEstados, limiteTransicoes) = Automato.Limites(texto.Length);
        Assert.True(automato.Estados <= limiteEstados);
        Assert.True(automato.Transicoes <= limiteTransicoes);
    }

    /// <summary>
    /// A maior subcadeia COMUM: o automato resolve em tempo linear no segundo
    /// texto, e o vetor de sufixos precisaria concatenar os dois.
    /// </summary>
    [Fact]
    public void AMaiorComumConcordaComAForcaBruta()
    {
        var sorteio = new Random(11);
        for (var tentativa = 0; tentativa < 200; tentativa++)
        {
            string Sortear(int n) => new(Enumerable.Range(0, n)
                .Select(_ => (char)('a' + sorteio.Next(3))).ToArray());

            var primeiro = Sortear(sorteio.Next(1, 25));
            var segundo = Sortear(sorteio.Next(1, 25));

            var esperado = Juiz.MaiorComum(primeiro, segundo).Length;
            Assert.Equal(esperado, new Automato(primeiro).MaiorComum(segundo).Length);
        }
    }

    /// <summary>
    /// O Kasai e LINEAR e a contagem um a um e quadratica, e a diferenca so
    /// aparece em texto com muita repeticao.
    ///
    /// No texto todo igual, a contagem direta compara da ordem de n²/2
    /// caracteres; o Kasai compara n. O teste mede a quantidade de comparacoes
    /// dos dois caminhos num texto onde isso importa.
    /// </summary>
    [Fact]
    public void OKasaiNaoDependeDaRepeticaoEAContagemDiretaSim()
    {
        const int n = 2_000;
        var repetido = new string('a', n);
        var vetor = new Vetor(repetido);

        // A contagem direta, com um contador de comparacoes.
        long comparacoes = 0;
        var direto = new int[n];
        for (var i = 1; i < n; i++)
        {
            var a = vetor.Sufixos[i - 1];
            var b = vetor.Sufixos[i];
            var quantos = 0;
            while (a + quantos < n && b + quantos < n && repetido[a + quantos] == repetido[b + quantos])
            {
                quantos++; comparacoes++;
            }
            comparacoes++;
            direto[i] = quantos;
        }

        Assert.Equal(direto, vetor.Prefixos);
        // A contagem direta gastou da ordem de n²/2.
        Assert.True(comparacoes > (long)n * n / 4, $"so {comparacoes} comparacoes");
    }
}
