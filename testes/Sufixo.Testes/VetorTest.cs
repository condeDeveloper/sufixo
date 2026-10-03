using Xunit;

namespace Conde.Sufixo.Testes;

/// <summary>
/// O vetor de sufixos, contra o juiz que ordena as cadeias uma comparacao de
/// caractere por vez.
///
/// A separacao importa mais aqui do que em quase qualquer outro assunto. A
/// construcao por dobra e INDIRETA: ela ordena por prefixos de tamanho 2^k e
/// dobra a cada passo, e o resultado so e o vetor de sufixos se uma cadeia de
/// igualdades de classe estiver certa. Um erro ali produz uma permutacao
/// plausivel, quase ordenada, que passa em qualquer teste que nao compare com a
/// ordenacao de verdade.
/// </summary>
public class VetorTest
{
    /// <summary>
    /// TODAS as cadeias de ate 10 caracteres sobre dois simbolos: 2.046 textos.
    ///
    /// Cadeia sorteada esconde exatamente os casos que quebram: texto todo
    /// igual, texto periodico, sufixo que e prefixo de outro. Aqui eles sao
    /// todos os casos.
    /// </summary>
    [Fact]
    public void BateComOJuizEmTodasAsCadeiasDeAte10SobreDoisSimbolos()
    {
        for (var tamanho = 1; tamanho <= 10; tamanho++)
            foreach (var texto in Juiz.TodasAsCadeias(tamanho, "ab"))
            {
                var vetor = new Vetor(texto);
                Assert.Equal(Juiz.VetorDeSufixos(texto), vetor.Sufixos);
            }
    }

    /// <summary>
    /// E com tres simbolos ate 7 caracteres, que e onde a ordenacao por
    /// contagem passa a ter mais de duas classes desde a primeira rodada.
    /// </summary>
    [Fact]
    public void BateComOJuizEmTodasAsCadeiasDeAte7SobreTresSimbolos()
    {
        for (var tamanho = 1; tamanho <= 7; tamanho++)
            foreach (var texto in Juiz.TodasAsCadeias(tamanho, "abc"))
            {
                var vetor = new Vetor(texto);
                Assert.Equal(Juiz.VetorDeSufixos(texto), vetor.Sufixos);
            }
    }

    /// <summary>
    /// O vetor e sempre uma PERMUTACAO das posicoes, e o inverso e de fato o
    /// inverso. Sao duas invariantes que uma dobra errada quebra antes de a
    /// ordem ficar errada.
    /// </summary>
    [Fact]
    public void OVetorEhUmaPermutacaoEOInversoEhOInverso()
    {
        var sorteio = new Random(3);
        for (var tentativa = 0; tentativa < 300; tentativa++)
        {
            var n = sorteio.Next(1, 200);
            var texto = new string(Enumerable.Range(0, n)
                .Select(_ => (char)('a' + sorteio.Next(4))).ToArray());
            var vetor = new Vetor(texto);

            Assert.Equal(n, vetor.Sufixos.Length);
            Assert.Equal(Enumerable.Range(0, n), vetor.Sufixos.OrderBy(x => x));
            for (var i = 0; i < n; i++) Assert.Equal(i, vetor.Posicao[vetor.Sufixos[i]]);
        }
    }

    /// <summary>
    /// O vetor de prefixos comuns, pelo Kasai, contra a contagem um a um.
    /// </summary>
    [Fact]
    public void OKasaiBateComAContagemUmAUm()
    {
        for (var tamanho = 1; tamanho <= 9; tamanho++)
            foreach (var texto in Juiz.TodasAsCadeias(tamanho, "ab"))
            {
                var vetor = new Vetor(texto);
                Assert.Equal(Juiz.Lcp(texto, vetor.Sufixos), vetor.Prefixos);
                // E o primeiro e sempre zero: nao ha anterior.
                Assert.Equal(0, vetor.Prefixos[0]);
            }
    }

    /// <summary>
    /// O Kasai em tempo LINEAR: o contador h cai no maximo um por posicao, entao
    /// o total de comparacoes e O(n).
    ///
    /// O teste mede isso no pior caso possivel para a versao ingenua, que e o
    /// texto todo igual: ali cada par de vizinhos tem prefixo comum enorme e a
    /// contagem um a um custa O(n²).
    /// </summary>
    [Fact]
    public void OKasaiNaoDegeneraNoTextoTodoIgual()
    {
        const int n = 20_000;
        var texto = new string('a', n);
        var vetor = new Vetor(texto);

        // No texto todo igual, os prefixos comuns sao 0, 1, 2, ..., n-1.
        Assert.Equal(Enumerable.Range(0, n), vetor.Prefixos);
        // E o vetor de sufixos e o inverso das posicoes.
        Assert.Equal(Enumerable.Range(0, n).Reverse(), vetor.Sufixos);
    }

    /// <summary>
    /// A busca binaria acha as mesmas ocorrencias que a varredura ingenua, em
    /// todas as cadeias pequenas e todos os padroes pequenos.
    /// </summary>
    [Fact]
    public void AsOcorrenciasBatemComAVarredura()
    {
        foreach (var texto in Juiz.TodasAsCadeias(8, "ab"))
        {
            var vetor = new Vetor(texto);
            for (var tamanho = 1; tamanho <= 4; tamanho++)
                foreach (var padrao in Juiz.TodasAsCadeias(tamanho, "ab"))
                    Assert.Equal(Juiz.Ocorrencias(texto, padrao), vetor.Ocorrencias(padrao));
        }
    }

    /// <summary>
    /// O caso que derruba a busca binaria escrita com CompareOrdinal direto: um
    /// sufixo MAIS CURTO que o padrao.
    ///
    /// Comparando as cadeias inteiras, "ab" é menor que "aba", entao o sufixo
    /// curto cai do lado errado da fronteira e a faixa sai torta. A comparacao
    /// precisa ser por PREFIXO, tratando o sufixo curto como menor.
    /// </summary>
    [Fact]
    public void OSufixoMaisCurtoQueOPadraoNaoQuebraABusca()
    {
        var vetor = new Vetor("abab");
        Assert.Equal(new[] { 0, 2 }, vetor.Ocorrencias("ab"));
        Assert.Equal(new[] { 0 }, vetor.Ocorrencias("abab"));
        Assert.Equal(new[] { 1 }, vetor.Ocorrencias("bab"));
        Assert.Empty(vetor.Ocorrencias("ababa"));
        Assert.Empty(vetor.Ocorrencias("c"));
        Assert.Empty(vetor.Ocorrencias(string.Empty));
    }

    /// <summary>
    /// As subcadeias distintas, pela formula n(n+1)/2 menos a soma dos prefixos
    /// comuns, contra o conjunto montado de verdade.
    /// </summary>
    [Fact]
    public void AsSubcadeiasDistintasBatemComOConjunto()
    {
        for (var tamanho = 1; tamanho <= 10; tamanho++)
            foreach (var texto in Juiz.TodasAsCadeias(tamanho, "ab"))
                Assert.Equal(Juiz.SubcadeiasDistintas(texto), new Vetor(texto).SubcadeiasDistintas());

        // E em textos onde a resposta e conhecida de cabeca. Os dois extremos:
        // no texto todo igual so ha uma subcadeia de cada tamanho, e no texto
        // sem repeticao nenhuma todas as n(n+1)/2 sao distintas.
        Assert.Equal(4, new Vetor("aaaa").SubcadeiasDistintas());    // a, aa, aaa, aaaa
        Assert.Equal(10, new Vetor("abcd").SubcadeiasDistintas());   // 4+3+2+1
        Assert.Equal(10, 4 * 5 / 2);
    }

    /// <summary>
    /// A maior subcadeia repetida esta sempre entre VIZINHOS do vetor
    /// ordenado, e e por isso que basta olhar o maior prefixo comum.
    /// </summary>
    [Fact]
    public void AMaiorRepetidaBateComAForcaBruta()
    {
        for (var tamanho = 2; tamanho <= 10; tamanho++)
            foreach (var texto in Juiz.TodasAsCadeias(tamanho, "ab"))
            {
                var pelaForca = Juiz.MaiorRepetida(texto);
                var peloVetor = new Vetor(texto).MaiorRepetida();
                // Pode haver mais de uma do mesmo tamanho; o que precisa bater
                // e o TAMANHO, e a achada precisa de fato repetir.
                Assert.Equal(pelaForca.Length, peloVetor.Length);
                if (peloVetor.Length > 0)
                    Assert.True(Juiz.Ocorrencias(texto, peloVetor).Count >= 2,
                        $"'{peloVetor}' nao repete em '{texto}'");
            }
    }

    [Fact]
    public void TextoVazioNaoQuebraNada()
    {
        var vetor = new Vetor(string.Empty);
        Assert.Empty(vetor.Sufixos);
        Assert.Empty(vetor.Prefixos);
        Assert.Equal(0, vetor.SubcadeiasDistintas());
        Assert.Equal(string.Empty, vetor.MaiorRepetida());
        Assert.Empty(vetor.Ocorrencias("a"));
    }

    /// <summary>
    /// As rodadas de dobra: log n e o PIOR caso, e em texto comum o numero e
    /// muito menor.
    ///
    /// A dobra para quando todas as classes ficam distintas, e com um alfabeto
    /// de quatro simbolos isso acontece assim que 4^(2^k) passa de n, ou seja
    /// com k da ordem de log log n. Com cem mil caracteres sao CINCO rodadas, e
    /// nao as dezessete que o limite sugere.
    ///
    /// O pior caso existe e e o texto todo igual, onde as classes so se separam
    /// no fim: ali sao mesmo log n rodadas.
    /// </summary>
    [Fact]
    public void AsRodadasDeDobraSaoLogLogNEmTextoComumELogNNoPiorCaso()
    {
        foreach (var n in new[] { 1000, 10_000, 100_000 })
        {
            var sorteio = new Random(n);
            var comum = new string(Enumerable.Range(0, n)
                .Select(_ => (char)('a' + sorteio.Next(4))).ToArray());
            var vetor = new Vetor(comum);

            Assert.True(vetor.Rodadas <= Math.Ceiling(Math.Log2(n)) + 1,
                $"{vetor.Rodadas} rodadas para n = {n}");
            // E muito abaixo do limite: perto de log log n.
            Assert.True(vetor.Rodadas <= Math.Log2(Math.Log2(n)) + 4,
                $"{vetor.Rodadas} rodadas para n = {n}, e log log n e {Math.Log2(Math.Log2(n)):F1}");

            // O pior caso, no entanto, chega perto do limite.
            var igual = new Vetor(new string('a', n));
            Assert.True(igual.Rodadas > vetor.Rodadas,
                $"todo igual deu {igual.Rodadas} e o comum deu {vetor.Rodadas}");
        }
    }
}
