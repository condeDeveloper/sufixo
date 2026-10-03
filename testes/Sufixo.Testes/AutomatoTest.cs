using Xunit;

namespace Conde.Sufixo.Testes;

/// <summary>
/// O automato de sufixos, e o limite de 2n-1 estados que o torna possivel.
/// </summary>
public class AutomatoTest
{
    /// <summary>
    /// O automato reconhece EXATAMENTE as subcadeias do texto: todas elas, e
    /// nenhuma a mais.
    ///
    /// A segunda metade e a que pega o erro de verdade. Pular a clonagem produz
    /// um automato que reconhece tudo o que deveria E mais um pouco, e um teste
    /// que so confere as subcadeias de verdade passa.
    /// </summary>
    [Fact]
    public void ReconheceExatamenteAsSubcadeias()
    {
        for (var tamanho = 1; tamanho <= 9; tamanho++)
            foreach (var texto in Juiz.TodasAsCadeias(tamanho, "ab"))
            {
                var automato = new Automato(texto);

                // Todas as subcadeias sao aceitas.
                for (var i = 0; i < texto.Length; i++)
                    for (var j = i + 1; j <= texto.Length; j++)
                        Assert.True(automato.Contem(texto[i..j]),
                            $"'{texto}' nao reconheceu a propria subcadeia '{texto[i..j]}'");

                // E nenhuma cadeia que NAO e subcadeia passa.
                for (var candidato = 1; candidato <= Math.Min(tamanho + 1, 6); candidato++)
                    foreach (var cadeia in Juiz.TodasAsCadeias(candidato, "ab"))
                        Assert.Equal(texto.Contains(cadeia, StringComparison.Ordinal), automato.Contem(cadeia));
            }
    }

    /// <summary>
    /// Os limites provados: no maximo 2n-1 estados e 3n-4 transicoes, qualquer
    /// que seja o texto.
    ///
    /// E um resultado surpreendente: um texto de um milhao de caracteres tem da
    /// ordem de 10^11 subcadeias com repeticao, e todas cabem num automato de
    /// dois milhoes de estados.
    /// </summary>
    [Fact]
    public void OsLimitesProvadosSaoRespeitados()
    {
        for (var tamanho = 1; tamanho <= 11; tamanho++)
            foreach (var texto in Juiz.TodasAsCadeias(tamanho, "ab"))
            {
                var automato = new Automato(texto);
                var (estados, transicoes) = Automato.Limites(tamanho);
                Assert.True(automato.Estados <= estados,
                    $"'{texto}': {automato.Estados} estados, limite {estados}");
                Assert.True(automato.Transicoes <= transicoes,
                    $"'{texto}': {automato.Transicoes} transicoes, limite {transicoes}");
            }
    }

    /// <summary>
    /// E os limites sao JUSTOS: existe texto que os alcanca.
    ///
    /// O texto que maximiza os estados e "abb...b", e o teste procura em vez de
    /// afirmar. Se o limite nao fosse justo, nenhum texto chegaria nele e a
    /// afirmacao do README seria mais fraca do que parece.
    /// </summary>
    [Fact]
    public void OsLimitesSaoJustos()
    {
        var achouEstados = false;
        for (var tamanho = 3; tamanho <= 10 && !achouEstados; tamanho++)
        {
            var (limite, _) = Automato.Limites(tamanho);
            foreach (var texto in Juiz.TodasAsCadeias(tamanho, "ab"))
                if (new Automato(texto).Estados == limite) { achouEstados = true; break; }
        }
        Assert.True(achouEstados, "nenhum texto alcancou o limite de estados");

        // O texto conhecido: "abb...b" alcanca 2n-1.
        foreach (var n in new[] { 4, 6, 10 })
        {
            var texto = "a" + new string('b', n - 1);
            var (limite, _) = Automato.Limites(n);
            Assert.Equal(limite, new Automato(texto).Estados);
        }
    }

    /// <summary>
    /// O texto todo igual e o caso MINIMO: nao ha clonagem nenhuma, e o
    /// automato vira uma linha de n+1 estados.
    /// </summary>
    [Fact]
    public void OTextoTodoIgualNaoCloneNada()
    {
        foreach (var n in new[] { 1, 5, 50, 500 })
        {
            var automato = new Automato(new string('a', n));
            Assert.Equal(n + 1, automato.Estados);
            Assert.Equal(n, automato.Transicoes);
            Assert.Equal(0, automato.Clones);
            // E ele tem exatamente n subcadeias distintas: a, aa, aaa...
            Assert.Equal(n, automato.SubcadeiasDistintas());
        }
    }

    /// <summary>
    /// A CLONAGEM acontece de verdade, e nos textos onde ela e necessaria.
    ///
    /// Se nenhum teste produzisse clone, o ramo mais dificil da construcao nunca
    /// teria sido executado e a suite estaria passando sem exercitar nada.
    /// </summary>
    [Fact]
    public void AClonagemAcontece()
    {
        var comClone = 0;
        foreach (var texto in Juiz.TodasAsCadeias(8, "ab"))
            if (new Automato(texto).Clones > 0) comClone++;

        Assert.True(comClone > 100, $"so {comClone} das 256 cadeias de 8 produziram clone");
        // E no caso classico: "abcbc" precisa clonar.
        Assert.True(new Automato("abcbc").Clones > 0);
    }

    /// <summary>
    /// As subcadeias distintas, pela soma sobre os estados, contra o conjunto
    /// montado de verdade E contra o vetor de sufixos.
    ///
    /// Tres caminhos sem nada em comum precisam dar o mesmo numero.
    /// </summary>
    [Fact]
    public void AsSubcadeiasDistintasConcordamPorTresCaminhos()
    {
        for (var tamanho = 1; tamanho <= 10; tamanho++)
            foreach (var texto in Juiz.TodasAsCadeias(tamanho, "ab"))
            {
                var esperado = Juiz.SubcadeiasDistintas(texto);
                Assert.Equal(esperado, new Automato(texto).SubcadeiasDistintas());
                Assert.Equal(esperado, new Vetor(texto).SubcadeiasDistintas());
            }
    }

    /// <summary>
    /// A maior subcadeia comum com outro texto, contra a forca bruta.
    /// </summary>
    [Fact]
    public void AMaiorComumBateComAForcaBruta()
    {
        var sorteio = new Random(3);
        for (var tentativa = 0; tentativa < 400; tentativa++)
        {
            string Sortear(int n) => new(Enumerable.Range(0, n)
                .Select(_ => (char)('a' + sorteio.Next(3))).ToArray());

            var primeiro = Sortear(sorteio.Next(1, 20));
            var segundo = Sortear(sorteio.Next(1, 20));

            var pelaForca = Juiz.MaiorComum(primeiro, segundo);
            var peloAutomato = new Automato(primeiro).MaiorComum(segundo);

            Assert.Equal(pelaForca.Length, peloAutomato.Length);
            if (peloAutomato.Length > 0)
            {
                Assert.Contains(peloAutomato, primeiro, StringComparison.Ordinal);
                Assert.Contains(peloAutomato, segundo, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void AMaiorComumEmCasosConhecidos()
    {
        Assert.Equal("abc", new Automato("xabcy").MaiorComum("zabcw"));
        Assert.Equal(string.Empty, new Automato("aaa").MaiorComum("bbb"));
        Assert.Equal("aaa", new Automato("aaa").MaiorComum("aaa"));
        Assert.Equal(string.Empty, new Automato("abc").MaiorComum(string.Empty));
    }

    /// <summary>
    /// O alfabeto MAIOR sai mais barato, o contrario do que se espera.
    ///
    /// O que infla o automato e a CLONAGEM, e ela acontece quando um mesmo
    /// trecho aparece em contextos diferentes. Com poucos simbolos tudo se
    /// repete e quase todo estado precisa ser partido; com alfabeto grande as
    /// repeticoes somem e os clones tambem.
    ///
    /// E o espantoso: com dois simbolos e cem mil caracteres, o automato guarda
    /// cinco bilhoes de subcadeias distintas em duzentos mil estados.
    /// </summary>
    [Fact]
    public void OAlfabetoMaiorDaMenosEstados()
    {
        const int n = 30_000;
        var anteriores = int.MaxValue;
        var anterioresClones = int.MaxValue;

        foreach (var alfabeto in new[] { 2, 4, 26 })
        {
            var sorteio = new Random(3);
            var texto = new string(Enumerable.Range(0, n)
                .Select(_ => (char)('a' + sorteio.Next(alfabeto))).ToArray());
            var automato = new Automato(texto);

            Assert.True(automato.Estados < anteriores,
                $"alfabeto {alfabeto}: {automato.Estados} estados, antes {anteriores}");
            Assert.True(automato.Clones < anterioresClones,
                $"alfabeto {alfabeto}: {automato.Clones} clones, antes {anterioresClones}");
            anteriores = automato.Estados;
            anterioresClones = automato.Clones;

            // E ele representa MAIS subcadeias com menos estados.
            Assert.True(automato.SubcadeiasDistintas() > automato.Estados * 100L);
        }
    }

    [Fact]
    public void TextoVazioNaoQuebraNada()
    {
        var automato = new Automato(string.Empty);
        Assert.Equal(1, automato.Estados);
        Assert.Equal(0, automato.Transicoes);
        Assert.Equal(0, automato.SubcadeiasDistintas());
        Assert.True(automato.Contem(string.Empty));
        Assert.False(automato.Contem("a"));
    }
}
