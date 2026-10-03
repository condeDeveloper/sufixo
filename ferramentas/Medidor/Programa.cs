using System.Diagnostics;
using Conde.Sufixo;

namespace Conde.Sufixo.Medidor;

/// <summary>
/// As medidas. O que se mede e TAMANHO DE ESTRUTURA e CONTAGEM DE OPERACAO, que
/// sao propriedades do algoritmo e dao o mesmo numero em qualquer maquina.
/// </summary>
public static class Programa
{
    public static int Main(string[] argumentos)
    {
        var qual = argumentos.Length > 0 ? argumentos[0] : "tudo";
        switch (qual)
        {
            case "automato": Automatos(); break;
            case "bwt": Transformada(); break;
            case "kasai": Kasais(); break;
            case "tamanho": Tamanhos(); break;
            case "tudo": Automatos(); Transformada(); Kasais(); Tamanhos(); break;
            default:
                Console.Error.WriteLine("medidas: automato, bwt, kasai, tamanho, tudo");
                return 1;
        }
        return 0;
    }

    private static string Sortear(int n, int alfabeto, int semente)
    {
        var sorteio = new Random(semente);
        return new string(Enumerable.Range(0, n).Select(_ => (char)('a' + sorteio.Next(alfabeto))).ToArray());
    }

    /// <summary>
    /// O limite de 2n-1 estados do automato de sufixos, conferido em textos de
    /// formatos bem diferentes.
    ///
    /// E um resultado que continua espantando: um texto de cem mil caracteres
    /// tem bilhoes de subcadeias com repeticao, e o automato que reconhece
    /// exatamente elas tem menos de duzentos mil estados.
    /// </summary>
    private static void Automatos()
    {
        const int n = 100_000;
        Console.WriteLine($"== o automato de sufixos, com {n:N0} caracteres ==");
        Console.WriteLine($"{"texto",-24}{"estados",12}{"limite 2n-1",14}{"transicoes",14}{"clones",10}{"subcadeias",18}");

        foreach (var (nome, texto) in new[]
        {
            ("todo igual", new string('a', n)),
            ("dois simbolos", Sortear(n, 2, 3)),
            ("quatro simbolos", Sortear(n, 4, 3)),
            ("vinte e seis", Sortear(n, 26, 3)),
            ("periodico de 10", string.Concat(Enumerable.Repeat("abcdefghij", n / 10))),
            ("a mais b...b", "a" + new string('b', n - 1)),
        })
        {
            var automato = new Automato(texto);
            var (limite, _) = Automato.Limites(texto.Length);
            Console.WriteLine($"{nome,-24}{automato.Estados,12:N0}{limite,14:N0}" +
                              $"{automato.Transicoes,14:N0}{automato.Clones,10:N0}{automato.SubcadeiasDistintas(),18:N0}");
        }
        Console.WriteLine();
        Console.WriteLine("a coluna da direita e o que o automato representa; a da esquerda e o que ele");
        Console.WriteLine("custa, e a relacao entre as duas e o assunto: com dois simbolos ele guarda");
        Console.WriteLine("cinco bilhoes de subcadeias em duzentos mil estados.");
        Console.WriteLine();
        Console.WriteLine("E o alfabeto MAIOR sai mais barato, o contrario do que eu esperava: 127 mil");
        Console.WriteLine("estados com vinte e seis simbolos contra 200 mil com dois. O que infla o");
        Console.WriteLine("automato e a CLONAGEM, e ela acontece quando um mesmo trecho aparece em");
        Console.WriteLine("contextos diferentes. Com poucos simbolos tudo se repete e quase todo estado");
        Console.WriteLine("precisa ser partido; com alfabeto grande as repeticoes somem e os clones");
        Console.WriteLine("tambem, de 99.968 para 27.569.");
        Console.WriteLine();
    }

    /// <summary>
    /// O que a transformada de Burrows e Wheeler faz de verdade: agrupar os
    /// iguais. E em que tipo de texto ela nao faz nada.
    /// </summary>
    private static void Transformada()
    {
        Console.WriteLine("== a transformada de Burrows e Wheeler, em 50.000 caracteres ==");
        Console.WriteLine($"{"texto",-24}{"blocos antes",14}{"blocos depois",16}{"reducao",12}");

        const int n = 50_000;
        var frase = "o rato roeu a roupa do rei de roma. ";
        foreach (var (nome, texto) in new[]
        {
            ("frase repetida", string.Concat(Enumerable.Repeat(frase, n / frase.Length + 1))[..n]),
            ("periodico de 10", string.Concat(Enumerable.Repeat("abcdefghij", n / 10))),
            ("dois simbolos", Sortear(n, 2, 5)),
            ("vinte e seis", Sortear(n, 26, 5)),
            ("todo igual", new string('a', n)),
        })
        {
            var (transformada, linha) = Bwt.Transformar(texto);
            if (Bwt.Inverter(transformada, linha) != texto)
                throw new InvalidOperationException($"a inversa falhou em '{nome}'");

            var antes = Bwt.Blocos(texto);
            var depois = Bwt.Blocos(transformada);
            Console.WriteLine($"{nome,-24}{antes,14:N0}{depois,16:N0}{1 - depois / (double)antes,12:P1}");
        }
        Console.WriteLine();
        Console.WriteLine("em texto com estrutura ela junta os iguais e da o que comprimir; em texto");
        Console.WriteLine("sorteado sobre alfabeto grande nao ha contexto repetido e ela nao ajuda nada");
        Console.WriteLine();
    }

    /// <summary>
    /// O Kasai contra a contagem um a um, em comparacoes de caractere.
    ///
    /// Os dois dao o mesmo vetor. O que muda e o custo, e ele so diverge em
    /// texto com muita repeticao, que e exatamente o caso que um teste com
    /// texto sorteado nao gera.
    /// </summary>
    private static void Kasais()
    {
        Console.WriteLine("== prefixos comuns: Kasai contra a contagem um a um ==");
        Console.WriteLine($"{"texto",-24}{"n",10}{"comp. direta",16}{"comp. Kasai",14}{"razao",10}");

        const int n = 20_000;
        foreach (var (nome, texto) in new[]
        {
            ("todo igual", new string('a', n)),
            ("periodico de 10", string.Concat(Enumerable.Repeat("abcdefghij", n / 10))),
            ("dois simbolos", Sortear(n, 2, 7)),
            ("vinte e seis", Sortear(n, 26, 7)),
        })
        {
            var vetor = new Vetor(texto);

            long direta = 0;
            for (var i = 1; i < n; i++)
            {
                int a = vetor.Sufixos[i - 1], b = vetor.Sufixos[i], quantos = 0;
                while (a + quantos < n && b + quantos < n && texto[a + quantos] == texto[b + quantos])
                {
                    quantos++; direta++;
                }
                direta++;
            }

            // O Kasai compara no maximo 2n vezes: h sobe n vezes no total e cai
            // no maximo uma por posicao.
            long kasai = 2L * n;
            Console.WriteLine($"{nome,-24}{n,10:N0}{direta,16:N0}{kasai,14:N0}{direta / (double)kasai,10:F1}x");
        }
        Console.WriteLine();
        Console.WriteLine("a coluna do Kasai e o LIMITE provado, 2n, e nao uma medida: o contador h sobe");
        Console.WriteLine("n vezes no total e cai no maximo uma por posicao, entao nao ha o que medir");
        Console.WriteLine();
    }

    /// <summary>
    /// Quanto cada estrutura custa em memoria e em construcao.
    /// </summary>
    private static void Tamanhos()
    {
        Console.WriteLine("== custo de construcao, em 100.000 caracteres sobre quatro simbolos ==");
        Console.WriteLine($"{"estrutura",-24}{"memoria",16}{"construcao",14}");

        const int n = 100_000;
        var texto = Sortear(n, 4, 11);

        var relogio = Stopwatch.StartNew();
        var vetor = new Vetor(texto);
        relogio.Stop();
        Console.WriteLine($"{"vetor de sufixos",-24}{3 * n + " inteiros",16}{relogio.ElapsedMilliseconds + " ms",14}");

        relogio.Restart();
        var automato = new Automato(texto);
        relogio.Stop();
        Console.WriteLine($"{"automato de sufixos",-24}{automato.Estados + " estados",16}{relogio.ElapsedMilliseconds + " ms",14}");

        relogio.Restart();
        var z = Zeta.Calcular(texto);
        relogio.Stop();
        Console.WriteLine($"{"funcao Z",-24}{n + " inteiros",16}{relogio.ElapsedMilliseconds + " ms",14}");

        relogio.Restart();
        var raios = Palindromo.Raios(texto);
        relogio.Stop();
        Console.WriteLine($"{"Manacher",-24}{raios.Length + " inteiros",16}{relogio.ElapsedMilliseconds + " ms",14}");

        Console.WriteLine();
        Console.WriteLine($"rodadas de dobra do vetor: {vetor.Rodadas}, e log2({n:N0}) e {Math.Log2(n):F1}");
        Console.WriteLine($"maior subcadeia repetida: {vetor.MaiorRepetida().Length} caracteres");
        Console.WriteLine($"maior palindromo: {Palindromo.Maior(texto).Length} caracteres");
        Console.WriteLine($"subcadeias distintas: {vetor.SubcadeiasDistintas():N0} de " +
                          $"{(long)n * (n + 1) / 2:N0} com repeticao");
        Console.WriteLine($"a funcao Z do texto inteiro vale {z[0]:N0}, que e o proprio n");
        Console.WriteLine();
        Console.WriteLine("os tempos vao so como referencia: eles dependem da maquina, e o que nao");
        Console.WriteLine("depende sao as contagens das outras medidas");
        Console.WriteLine();
    }
}
