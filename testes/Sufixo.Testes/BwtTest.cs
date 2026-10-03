using Xunit;

namespace Conde.Sufixo.Testes;

/// <summary>
/// A transformada de Burrows e Wheeler: a permutacao que agrupa os iguais e que
/// se desfaz sozinha.
/// </summary>
public class BwtTest
{
    /// <summary>
    /// A transformada feita a partir do vetor de sufixos bate com a definicao,
    /// que monta a matriz de rotacoes e ordena.
    ///
    /// Sao caminhos sem nada em comum: um ordena n cadeias de tamanho n, o
    /// outro le a coluna de uma permutacao ja calculada.
    /// </summary>
    [Fact]
    public void BateComAMatrizDeRotacoesEmTodasAsCadeiasDeAte10()
    {
        for (var tamanho = 1; tamanho <= 10; tamanho++)
            foreach (var texto in Juiz.TodasAsCadeias(tamanho, "ab"))
            {
                var (pelaMatriz, linhaDaMatriz) = Bwt.PorForcaBruta(texto);
                var (pelaDobra, linha) = Bwt.Transformar(texto);
                Assert.Equal(pelaMatriz, pelaDobra);

                // A LINHA nao e comparada, e isso nao e desleixo. Quando o texto
                // e periodico, duas rotacoes sao a MESMA cadeia e a ordem entre
                // elas e arbitraria: em "aa" as duas linhas sao "aa", e dizer
                // que a original e a primeira ou a segunda e igualmente certo.
                // O que precisa valer e que a linha devolvida INVERTA, e e isso
                // que o teste confere.
                Assert.Equal(texto, Bwt.Inverter(pelaDobra, linha));
                Assert.Equal(texto, Bwt.Inverter(pelaMatriz, linhaDaMatriz));
            }
    }

    /// <summary>
    /// A IDA E VOLTA, que e a propriedade que faz a transformada valer alguma
    /// coisa: ela e invertivel guardando so um indice.
    /// </summary>
    [Fact]
    public void AIdaEVoltaDevolveOTextoEmTodasAsCadeiasDeAte12()
    {
        for (var tamanho = 1; tamanho <= 12; tamanho++)
            foreach (var texto in Juiz.TodasAsCadeias(tamanho, "ab"))
            {
                var (transformada, linha) = Bwt.Transformar(texto);
                Assert.Equal(texto, Bwt.Inverter(transformada, linha));
            }
    }

    /// <summary>E com tres simbolos, e com textos de verdade.</summary>
    [Fact]
    public void AIdaEVoltaFuncionaComAlfabetosMaiores()
    {
        for (var tamanho = 1; tamanho <= 7; tamanho++)
            foreach (var texto in Juiz.TodasAsCadeias(tamanho, "abc"))
            {
                var (transformada, linha) = Bwt.Transformar(texto);
                Assert.Equal(texto, Bwt.Inverter(transformada, linha));
            }

        foreach (var texto in new[]
        {
            "banana", "mississippi", "o rato roeu a roupa do rei de roma",
            "abracadabra", "aaaaaaaa", "a", "ab",
        })
        {
            var (transformada, linha) = Bwt.Transformar(texto);
            Assert.Equal(texto.Length, transformada.Length);
            Assert.Equal(texto, Bwt.Inverter(transformada, linha));
            // E a transformada e uma PERMUTACAO do texto: os mesmos caracteres,
            // em outra ordem.
            Assert.Equal(texto.OrderBy(c => c), transformada.OrderBy(c => c));
        }
    }

    /// <summary>
    /// O caso da banana, e o exemplo que quase todo material cita errado.
    ///
    /// "banana" vira "nnbaaa". O famoso "annb$aa" e a transformada de
    /// "banana$", com a sentinela que o indice FM precisa: com ela a matriz de
    /// rotacoes e outra, e a resposta tambem. Eu tinha escrito "annbaa" de
    /// cabeca, sem sentinela e sem cifrao, e o teste contra a matriz corrigiu.
    /// </summary>
    [Fact]
    public void OCasoDaBanana()
    {
        var (transformada, linha) = Bwt.Transformar("banana");
        Assert.Equal("nnbaaa", transformada);
        Assert.Equal("banana", Bwt.Inverter(transformada, linha));

        // E os iguais ficaram mais juntos: de seis blocos para tres.
        Assert.Equal(6, Bwt.Blocos("banana"));
        Assert.Equal(3, Bwt.Blocos(transformada));

        // Com a sentinela, a resposta e a publicada.
        var (comCifrao, _) = Bwt.Transformar("banana$");
        Assert.Equal("annb$aa", comCifrao);
    }

    /// <summary>
    /// O que a transformada de fato faz: AGRUPAR os iguais.
    ///
    /// Em texto com estrutura, a quantidade de blocos de caracteres repetidos
    /// cai bastante, e e so dai que vem a compressao do bzip2. Em texto
    /// sorteado nao ha o que agrupar, e o teste confere as duas coisas, para a
    /// afirmacao nao ficar mais forte do que a realidade.
    /// </summary>
    [Fact]
    public void ElaAgrupaOsIguaisEmTextoComEstruturaENaoEmTextoSorteado()
    {
        var comEstrutura = string.Concat(Enumerable.Repeat("o rato roeu a roupa do rei de roma. ", 40));
        var (transformado, _) = Bwt.Transformar(comEstrutura);
        var antes = Bwt.Blocos(comEstrutura);
        var depois = Bwt.Blocos(transformado);
        Assert.True(depois < antes * 0.6, $"de {antes} blocos para {depois}");

        var sorteio = new Random(3);
        var sorteado = new string(Enumerable.Range(0, comEstrutura.Length)
            .Select(_ => (char)('a' + sorteio.Next(26))).ToArray());
        var (transformadoSorteado, _) = Bwt.Transformar(sorteado);
        var antesSorteado = Bwt.Blocos(sorteado);
        var depoisSorteado = Bwt.Blocos(transformadoSorteado);
        // Aqui ela nao ajuda: nao ha contexto repetido para agrupar.
        Assert.True(depoisSorteado > antesSorteado * 0.9,
            $"em texto sorteado foi de {antesSorteado} para {depoisSorteado}");
    }

    /// <summary>
    /// A inversao depende de a ordenacao da primeira coluna ser ESTAVEL, e o
    /// teste exercita isso em textos com muitos caracteres repetidos, que e onde
    /// a estabilidade decide.
    /// </summary>
    [Fact]
    public void AInversaoFuncionaComMuitosRepetidos()
    {
        foreach (var texto in new[]
        {
            new string('a', 100),
            string.Concat(Enumerable.Repeat("ab", 50)),
            string.Concat(Enumerable.Repeat("aab", 40)),
            new string('a', 50) + new string('b', 50),
        })
        {
            var (transformada, linha) = Bwt.Transformar(texto);
            Assert.Equal(texto, Bwt.Inverter(transformada, linha));
        }
    }

    [Fact]
    public void EntradaInvalidaEhRecusada()
    {
        Assert.Equal((string.Empty, 0), Bwt.Transformar(string.Empty));
        Assert.Equal(string.Empty, Bwt.Inverter(string.Empty, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Bwt.Inverter("abc", -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Bwt.Inverter("abc", 3));
        Assert.Equal(0, Bwt.Blocos(string.Empty));
    }
}
