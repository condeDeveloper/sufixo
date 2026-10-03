using Xunit;

namespace Conde.Sufixo.Testes;

/// <summary>
/// A funcao Z e a funcao de prefixo, que carregam a mesma informacao por
/// caminhos sem nada em comum.
/// </summary>
public class ZetaTest
{
    /// <summary>
    /// Contra a definicao, em TODAS as cadeias de ate 12 caracteres sobre dois
    /// simbolos: 8.190 textos.
    ///
    /// Esse dominio nao e exagero. O erro que a funcao Z esconde e usar o valor
    /// reaproveitado sem limita-lo ao que sobra da janela, e ele so aparece em
    /// cadeias PERIODICAS, que um sorteio quase nunca gera.
    /// </summary>
    [Fact]
    public void BateComADefinicaoEmTodasAsCadeiasDeAte12()
    {
        for (var tamanho = 1; tamanho <= 12; tamanho++)
            foreach (var texto in Juiz.TodasAsCadeias(tamanho, "ab"))
                Assert.Equal(Zeta.PorForcaBruta(texto), Zeta.Calcular(texto));
    }

    /// <summary>
    /// As cadeias periodicas, que sao o caso que a janela mal limitada quebra.
    /// </summary>
    [Fact]
    public void AsCadeiasPeriodicasSaemCertas()
    {
        foreach (var texto in new[] { "aaaa", "abab", "aabaab", "abcabcabc", "aaabaaab", "ababab" })
        {
            Assert.Equal(Zeta.PorForcaBruta(texto), Zeta.Calcular(texto));
        }

        // E num caso conferido na mao: em "aaaa", Z = [4, 3, 2, 1].
        Assert.Equal(new[] { 4, 3, 2, 1 }, Zeta.Calcular("aaaa"));
        // Em "abab", Z = [4, 0, 2, 0].
        Assert.Equal(new[] { 4, 0, 2, 0 }, Zeta.Calcular("abab"));
    }

    /// <summary>
    /// Z[0] e o texto inteiro, por convencao, e os outros nunca passam do que
    /// sobra a partir da posicao.
    /// </summary>
    [Fact]
    public void OsValoresRespeitamOsLimites()
    {
        var sorteio = new Random(3);
        for (var tentativa = 0; tentativa < 500; tentativa++)
        {
            var n = sorteio.Next(1, 100);
            var texto = new string(Enumerable.Range(0, n)
                .Select(_ => (char)('a' + sorteio.Next(3))).ToArray());
            var z = Zeta.Calcular(texto);

            Assert.Equal(n, z[0]);
            for (var i = 1; i < n; i++)
            {
                Assert.InRange(z[i], 0, n - i);
                // E o que ele afirma e verdade: os z[i] primeiros caracteres
                // batem com o prefixo.
                Assert.Equal(texto[..z[i]], texto.Substring(i, z[i]));
                // E o seguinte NAO bate, senao o valor seria maior.
                if (i + z[i] < n) Assert.NotEqual(texto[z[i]], texto[i + z[i]]);
            }
        }
    }

    /// <summary>
    /// A busca por concatenacao acha as mesmas ocorrencias que a varredura.
    /// </summary>
    [Fact]
    public void AsOcorrenciasBatemComAVarredura()
    {
        foreach (var texto in Juiz.TodasAsCadeias(9, "ab"))
            for (var tamanho = 1; tamanho <= 4; tamanho++)
                foreach (var padrao in Juiz.TodasAsCadeias(tamanho, "ab"))
                    Assert.Equal(Juiz.Ocorrencias(texto, padrao), Zeta.Ocorrencias(texto, padrao));
    }

    /// <summary>
    /// O separador precisa ser um caractere que nao aparece em nenhum dos dois.
    ///
    /// Sem ele, uma ocorrencia pode atravessar a emenda e o resultado passa a
    /// ter posicoes que nao existem. O metodo recusa em vez de devolver lixo.
    /// </summary>
    [Fact]
    public void OSeparadorNaoPodeAparecerNosTextos()
    {
        Assert.Throws<ArgumentException>(() => Zeta.Ocorrencias("abc", "b", separador: 'b'));
        Assert.Throws<ArgumentException>(() => Zeta.Ocorrencias("abc", "x", separador: 'a'));
        // Com um separador que nao aparece, funciona.
        Assert.Equal(new[] { 1 }, Zeta.Ocorrencias("abc", "b", separador: '#'));
    }

    /// <summary>
    /// O menor periodo, contra a definicao: o menor p tal que repetir o pedaco
    /// de tamanho p reconstroi o texto.
    /// </summary>
    [Fact]
    public void OMenorPeriodoBateComADefinicao()
    {
        for (var tamanho = 1; tamanho <= 11; tamanho++)
            foreach (var texto in Juiz.TodasAsCadeias(tamanho, "ab"))
            {
                var p = Zeta.MenorPeriodo(texto);

                // A definicao: texto[i] == texto[i - p] para todo i a partir de p.
                for (var i = p; i < texto.Length; i++)
                    Assert.Equal(texto[i - p], texto[i]);

                // E nenhum periodo menor serve.
                for (var menor = 1; menor < p; menor++)
                {
                    var vale = true;
                    for (var i = menor; i < texto.Length && vale; i++)
                        if (texto[i - menor] != texto[i]) vale = false;
                    Assert.False(vale, $"'{texto}' tem periodo {menor}, menor que o devolvido {p}");
                }
            }
    }

    [Fact]
    public void OPeriodoEmCasosConhecidos()
    {
        Assert.Equal(1, Zeta.MenorPeriodo("aaaa"));
        Assert.Equal(2, Zeta.MenorPeriodo("abab"));
        Assert.Equal(2, Zeta.MenorPeriodo("ababa"));
        Assert.Equal(3, Zeta.MenorPeriodo("abcabc"));
        Assert.Equal(4, Zeta.MenorPeriodo("abcd"));
        Assert.Equal(0, Zeta.MenorPeriodo(string.Empty));

        // "e uma repeticao de alguma coisa" e o periodo dividir o tamanho.
        Assert.Equal(0, "abab".Length % Zeta.MenorPeriodo("abab"));
        Assert.NotEqual(0, "ababa".Length % Zeta.MenorPeriodo("ababa"));
    }

    /// <summary>
    /// A funcao de prefixo, contra a definicao: o maior prefixo proprio que
    /// tambem e sufixo.
    /// </summary>
    [Fact]
    public void AFuncaoDePrefixoBateComADefinicao()
    {
        for (var tamanho = 1; tamanho <= 11; tamanho++)
            foreach (var texto in Juiz.TodasAsCadeias(tamanho, "ab"))
            {
                var pi = Zeta.Prefixo(texto);
                for (var i = 0; i < texto.Length; i++)
                {
                    var pedaco = texto[..(i + 1)];
                    var esperado = 0;
                    for (var k = i; k >= 1; k--)
                        if (pedaco[..k] == pedaco[^k..]) { esperado = k; break; }
                    Assert.Equal(esperado, pi[i]);
                }
            }
    }

    /// <summary>
    /// As DUAS funcoes concordam, e esse e o melhor teste que existe para as
    /// duas: sao construcoes sem nada em comum.
    ///
    /// A relacao: ha uma borda de tamanho k no pedaco ate i exatamente quando
    /// Z[i-k+1] alcanca k.
    /// </summary>
    [Fact]
    public void AsDuasFuncoesConcordam()
    {
        foreach (var texto in Juiz.TodasAsCadeias(10, "ab"))
        {
            var z = Zeta.Calcular(texto);
            var pi = Zeta.Prefixo(texto);
            var n = texto.Length;

            for (var i = 0; i < n; i++)
            {
                var k = pi[i];
                if (k == 0) continue;
                // A borda de tamanho k comeca na posicao i-k+1 e vai ate i.
                Assert.True(z[i - k + 1] >= k,
                    $"'{texto}': pi[{i}] = {k} e z[{i - k + 1}] = {z[i - k + 1]}");
            }
        }
    }

    /// <summary>
    /// As bordas, do maior para o menor, contra a enumeracao direta.
    /// </summary>
    [Fact]
    public void AsBordasBatemComAEnumeracao()
    {
        for (var tamanho = 1; tamanho <= 11; tamanho++)
            foreach (var texto in Juiz.TodasAsCadeias(tamanho, "ab"))
            {
                var esperado = new List<int>();
                for (var k = texto.Length - 1; k >= 1; k--)
                    if (texto[..k] == texto[^k..]) esperado.Add(k);
                Assert.Equal(esperado, Zeta.Bordas(texto));
            }

        Assert.Equal(new[] { 3, 1 }, Zeta.Bordas("ababa"));
        Assert.Equal(new[] { 3, 2, 1 }, Zeta.Bordas("aaaa"));
        Assert.Empty(Zeta.Bordas("abc"));
        Assert.Empty(Zeta.Bordas(string.Empty));
    }

    [Fact]
    public void TextoVazioNaoQuebraNada()
    {
        Assert.Empty(Zeta.Calcular(string.Empty));
        Assert.Empty(Zeta.Prefixo(string.Empty));
        Assert.Empty(Zeta.Ocorrencias(string.Empty, "a"));
        Assert.Empty(Zeta.Ocorrencias("abc", string.Empty));
    }
}
