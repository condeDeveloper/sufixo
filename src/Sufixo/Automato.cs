namespace Conde.Sufixo;

/// <summary>
/// O automato de sufixos (DAWG), de Blumer e outros, 1985: o menor automato
/// deterministico que reconhece TODAS as subcadeias do texto.
///
/// O resultado que torna a estrutura possivel e surpreendente: esse automato
/// tem no maximo 2n-1 estados e 3n-4 transicoes, qualquer que seja o texto. Um
/// texto de um milhao de caracteres tem da ordem de 10^11 subcadeias com
/// repeticao, e todas cabem num automato de dois milhoes de estados.
///
/// A construcao e online, caractere por caractere, e gira em torno de uma peca
/// so: o LINK DE SUFIXO. Cada estado representa um conjunto de subcadeias que
/// terminam nas mesmas posicoes, e o link aponta para o estado do maior sufixo
/// proprio que termina em MAIS posicoes.
///
/// O caso que faz a construcao ter dois ramos, e que e onde todo mundo erra, e
/// a CLONAGEM. Quando o estado alcancado pela transicao nao e "compacto", ou
/// seja, quando o tamanho dele e maior que o do caminho que levou ate ele, ele
/// precisa ser partido em dois: o clone fica com as subcadeias curtas e o
/// original com as longas. Pular isso da um automato que reconhece coisas a
/// mais, e o teste so pega se comparar com o conjunto de subcadeias de verdade.
/// </summary>
public sealed class Automato
{
    private sealed class Estado
    {
        /// <summary>O tamanho da MAIOR subcadeia deste estado.</summary>
        public int Tamanho;

        /// <summary>O estado do maior sufixo proprio que vive em outro estado.</summary>
        public int Link = -1;

        public readonly Dictionary<char, int> Vai = [];

        /// <summary>Verdadeiro quando o estado nasceu de uma clonagem.</summary>
        public bool Clone;
    }

    private readonly List<Estado> estados = [];
    private int ultimo;

    public Automato(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        Texto = texto;
        estados.Add(new Estado { Tamanho = 0, Link = -1 });
        ultimo = 0;
        foreach (var letra in texto) Acrescentar(letra);
    }

    public string Texto { get; }

    public int Estados => estados.Count;

    public int Transicoes => estados.Sum(e => e.Vai.Count);

    public int Clones => estados.Count(e => e.Clone);

    private void Acrescentar(char letra)
    {
        var novo = estados.Count;
        estados.Add(new Estado { Tamanho = estados[ultimo].Tamanho + 1 });

        // Sobe pelos links enquanto nao houver transicao por esta letra.
        var p = ultimo;
        while (p != -1 && !estados[p].Vai.ContainsKey(letra))
        {
            estados[p].Vai[letra] = novo;
            p = estados[p].Link;
        }

        if (p == -1)
        {
            estados[novo].Link = 0;
        }
        else
        {
            var q = estados[p].Vai[letra];
            if (estados[p].Tamanho + 1 == estados[q].Tamanho)
            {
                // O estado alcancado ja e compacto: o link aponta direto.
                estados[novo].Link = q;
            }
            else
            {
                // A CLONAGEM. O estado q junta subcadeias de tamanhos que agora
                // precisam se separar: as de ate Tamanho(p)+1 passam a viver no
                // clone, e as maiores ficam em q.
                var clone = estados.Count;
                estados.Add(new Estado
                {
                    Tamanho = estados[p].Tamanho + 1,
                    Link = estados[q].Link,
                    Clone = true,
                });
                foreach (var (chave, destino) in estados[q].Vai) estados[clone].Vai[chave] = destino;

                while (p != -1 && estados[p].Vai.TryGetValue(letra, out var atual) && atual == q)
                {
                    estados[p].Vai[letra] = clone;
                    p = estados[p].Link;
                }
                estados[q].Link = clone;
                estados[novo].Link = clone;
            }
        }
        ultimo = novo;
    }

    /// <summary>Verdadeiro quando a cadeia e subcadeia do texto.</summary>
    public bool Contem(string cadeia)
    {
        ArgumentNullException.ThrowIfNull(cadeia);
        var atual = 0;
        foreach (var letra in cadeia)
        {
            if (!estados[atual].Vai.TryGetValue(letra, out var proximo)) return false;
            atual = proximo;
        }
        return true;
    }

    /// <summary>
    /// Quantas subcadeias DISTINTAS o texto tem.
    ///
    /// Cada estado representa as subcadeias de tamanho entre Tamanho(link)+1 e
    /// Tamanho(estado), e elas sao todas distintas e cobrem tudo sem repetir.
    /// Entao a conta e uma soma simples sobre os estados, sem percorrer caminho
    /// nenhum.
    /// </summary>
    public long SubcadeiasDistintas()
    {
        long total = 0;
        for (var i = 1; i < estados.Count; i++)
            total += estados[i].Tamanho - estados[estados[i].Link].Tamanho;
        return total;
    }

    /// <summary>
    /// A maior subcadeia comum com outro texto, passando-o pelo automato.
    ///
    /// Anda-se com o outro texto; quando a transicao falta, sobe-se pelos links
    /// ate achar uma, encurtando o casamento. O custo e linear no tamanho do
    /// outro texto, o que e o motivo de esta estrutura existir: comparar um
    /// texto com muitos outros fica linear em cada um, depois de uma construcao
    /// so.
    /// </summary>
    public string MaiorComum(string outro)
    {
        ArgumentNullException.ThrowIfNull(outro);
        int atual = 0, tamanho = 0, melhor = 0, onde = 0;

        for (var i = 0; i < outro.Length; i++)
        {
            var letra = outro[i];
            while (atual != 0 && !estados[atual].Vai.ContainsKey(letra))
            {
                atual = estados[atual].Link;
                tamanho = estados[atual].Tamanho;
            }
            if (estados[atual].Vai.TryGetValue(letra, out var proximo))
            {
                atual = proximo;
                tamanho++;
            }
            if (tamanho > melhor) { melhor = tamanho; onde = i; }
        }
        return melhor == 0 ? string.Empty : outro.Substring(onde - melhor + 1, melhor);
    }

    /// <summary>
    /// Os limites provados: no maximo 2n-1 estados e 3n-4 transicoes, para n de
    /// pelo menos 3.
    ///
    /// Eles nao sao estimativa e e contra eles que o teste confere o que mediu.
    /// </summary>
    public static (int Estados, int Transicoes) Limites(int tamanho) =>
        tamanho < 3 ? (2 * tamanho + 1, 3 * tamanho) : (2 * tamanho - 1, 3 * tamanho - 4);
}
