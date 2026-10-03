namespace Conde.Sufixo;

/// <summary>
/// O JUIZ: o vetor de sufixos montado ordenando as n cadeias, uma comparacao de
/// caractere por vez.
///
/// Custa O(n² log n) e e por isso que ele nao serve para nada alem de julgar.
/// Em compensacao, ele e a DEFINICAO: o vetor de sufixos e a lista das posicoes
/// ordenada pelos sufixos que comecam nelas, e e exatamente isso que este codigo
/// faz, sem dobra, sem contagem, sem radix.
///
/// Esta separacao importa mais aqui do que em quase qualquer outro assunto. Os
/// algoritmos rapidos de vetor de sufixos sao construcoes indiretas: eles
/// ordenam por prefixos de tamanho 2^k e dobram a cada passo, e o resultado so e
/// o vetor de sufixos se uma cadeia de igualdades de classe estiver certa. Um
/// erro ali produz uma permutacao plausivel, quase ordenada, que passa em
/// qualquer teste que nao compare com a ordenacao de verdade.
/// </summary>
public static class Juiz
{
    /// <summary>
    /// O vetor de sufixos pela definicao: ordena as posicoes comparando as
    /// cadeias.
    /// </summary>
    public static int[] VetorDeSufixos(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        var posicoes = Enumerable.Range(0, texto.Length).ToArray();
        Array.Sort(posicoes, (a, b) => string.CompareOrdinal(texto[a..], texto[b..]));
        return posicoes;
    }

    /// <summary>
    /// O maior prefixo comum entre dois sufixos, contado caractere por
    /// caractere.
    /// </summary>
    public static int MaiorPrefixoComum(string texto, int a, int b)
    {
        ArgumentNullException.ThrowIfNull(texto);
        var quantos = 0;
        while (a + quantos < texto.Length && b + quantos < texto.Length &&
               texto[a + quantos] == texto[b + quantos]) quantos++;
        return quantos;
    }

    /// <summary>
    /// O vetor de prefixos comuns entre vizinhos do vetor de sufixos, contado
    /// um a um.
    /// </summary>
    public static int[] Lcp(string texto, int[] sufixos)
    {
        ArgumentNullException.ThrowIfNull(texto);
        ArgumentNullException.ThrowIfNull(sufixos);
        var saida = new int[sufixos.Length];
        for (var i = 1; i < sufixos.Length; i++)
            saida[i] = MaiorPrefixoComum(texto, sufixos[i - 1], sufixos[i]);
        return saida;
    }

    /// <summary>Todas as ocorrencias de um padrao, por varredura ingenua.</summary>
    public static List<int> Ocorrencias(string texto, string padrao)
    {
        ArgumentNullException.ThrowIfNull(texto);
        ArgumentNullException.ThrowIfNull(padrao);
        var saida = new List<int>();
        if (padrao.Length == 0 || padrao.Length > texto.Length) return saida;
        for (var i = 0; i + padrao.Length <= texto.Length; i++)
            if (string.CompareOrdinal(texto, i, padrao, 0, padrao.Length) == 0) saida.Add(i);
        return saida;
    }

    /// <summary>
    /// Quantas subcadeias DISTINTAS o texto tem, montando o conjunto delas.
    ///
    /// Custa O(n²) subcadeias e so serve em texto pequeno; e justamente por nao
    /// usar nenhuma propriedade de sufixo que ele julga as formulas que usam.
    /// </summary>
    public static long SubcadeiasDistintas(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        var vistas = new HashSet<string>();
        for (var i = 0; i < texto.Length; i++)
            for (var j = i + 1; j <= texto.Length; j++)
                vistas.Add(texto[i..j]);
        return vistas.Count;
    }

    /// <summary>
    /// A maior subcadeia repetida, por forca bruta sobre todos os pares de
    /// posicoes.
    /// </summary>
    public static string MaiorRepetida(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        var melhor = string.Empty;
        for (var i = 0; i < texto.Length; i++)
            for (var j = i + 1; j < texto.Length; j++)
            {
                var quantos = MaiorPrefixoComum(texto, i, j);
                if (quantos > melhor.Length) melhor = texto.Substring(i, quantos);
            }
        return melhor;
    }

    /// <summary>
    /// A maior subcadeia comum a dois textos, por forca bruta.
    /// </summary>
    public static string MaiorComum(string primeiro, string segundo)
    {
        ArgumentNullException.ThrowIfNull(primeiro);
        ArgumentNullException.ThrowIfNull(segundo);
        var melhor = string.Empty;
        for (var i = 0; i < primeiro.Length; i++)
            for (var j = i + 1 + melhor.Length; j <= primeiro.Length; j++)
            {
                var pedaco = primeiro[i..j];
                if (segundo.Contains(pedaco, StringComparison.Ordinal)) melhor = pedaco;
            }
        return melhor;
    }

    /// <summary>
    /// Todas as cadeias de ate <paramref name="tamanho"/> caracteres sobre um
    /// alfabeto.
    ///
    /// Com alfabeto de dois e tamanho 10 sao 2.046 cadeias, o que cabe numa
    /// varredura exaustiva. Testar estruturas de sufixo em cadeias sorteadas
    /// esconde exatamente os casos que quebram: texto todo igual, texto
    /// periodico, texto com sufixo que e prefixo de outro.
    /// </summary>
    public static IEnumerable<string> TodasAsCadeias(int tamanho, string alfabeto)
    {
        ArgumentNullException.ThrowIfNull(alfabeto);
        var atual = new char[tamanho];
        IEnumerable<string> Andar(int i)
        {
            if (i == tamanho) { yield return new string(atual); yield break; }
            foreach (var letra in alfabeto)
            {
                atual[i] = letra;
                foreach (var cadeia in Andar(i + 1)) yield return cadeia;
            }
        }
        return Andar(0);
    }
}
