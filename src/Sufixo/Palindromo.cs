namespace Conde.Sufixo;

/// <summary>
/// O algoritmo de Manacher, 1975: todos os palindromos de um texto em tempo
/// LINEAR.
///
/// A forca bruta testa cada centro e expande, o que custa O(n²) e e o que quase
/// todo mundo escreve. O Manacher reaproveita: se ja se sabe que ha um
/// palindromo grande em torno de um centro c, entao qualquer posicao i dentro
/// dele tem um ESPELHO do outro lado, e o raio do espelho ja foi calculado.
///
/// O caso que decide tudo e quando o raio do espelho ENCOSTA na borda do
/// palindromo maior. Ali nao da para reaproveitar o valor inteiro, porque o que
/// esta fora da borda nunca foi comparado: e preciso cortar no que sobra e
/// continuar expandindo dali. Usar o valor do espelho sem esse corte da
/// respostas maiores que a verdade, e so em cadeias com estrutura, que e
/// exatamente o que um sorteio nao gera.
///
/// Os palindromos de tamanho par sao tratados com o truque da INTERCALACAO: o
/// texto "aba" vira "#a#b#a#", e nele todo palindromo fica de tamanho impar. E
/// um truque que apaga metade do codigo e metade dos erros.
/// </summary>
public static class Palindromo
{
    /// <summary>O separador intercalado. Nao pode aparecer no texto.</summary>
    public const char Separador = '';

    /// <summary>
    /// Para cada centro do texto intercalado, o RAIO do maior palindromo.
    ///
    /// O vetor tem 2n+1 posicoes: as pares sao os centros entre caracteres
    /// (palindromos de tamanho par) e as impares sao os caracteres (tamanho
    /// impar).
    /// </summary>
    public static int[] Raios(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        if (texto.Contains(Separador))
            throw new ArgumentException("o texto nao pode conter o separador", nameof(texto));

        var s = Intercalar(texto);
        var n = s.Length;
        var raio = new int[n];
        int centro = 0, direita = 0;

        for (var i = 0; i < n; i++)
        {
            // Dentro do palindromo atual: reaproveita o espelho, mas NUNCA alem
            // do que sobra ate a borda. Sem esse corte, cadeias com estrutura
            // dao respostas maiores que a verdade.
            if (i < direita) raio[i] = Math.Min(direita - i, raio[2 * centro - i]);

            while (i - raio[i] - 1 >= 0 && i + raio[i] + 1 < n && s[i - raio[i] - 1] == s[i + raio[i] + 1])
                raio[i]++;

            if (i + raio[i] > direita) { centro = i; direita = i + raio[i]; }
        }
        return raio;
    }

    /// <summary>O texto com o separador entre cada par de caracteres e nas pontas.</summary>
    public static string Intercalar(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        var saida = new char[2 * texto.Length + 1];
        for (var i = 0; i < saida.Length; i++)
            saida[i] = i % 2 == 0 ? Separador : texto[i / 2];
        return new string(saida);
    }

    /// <summary>
    /// O maior palindromo do texto.
    ///
    /// O raio no texto intercalado E o tamanho do palindromo no texto original,
    /// e essa coincidencia e justamente o que o truque compra.
    /// </summary>
    public static string Maior(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        if (texto.Length == 0) return string.Empty;

        var raio = Raios(texto);
        var melhor = 0;
        var onde = 0;
        for (var i = 0; i < raio.Length; i++)
            if (raio[i] > melhor) { melhor = raio[i]; onde = i; }

        var inicio = (onde - melhor) / 2;
        return texto.Substring(inicio, melhor);
    }

    /// <summary>
    /// Quantas subcadeias palindromicas o texto tem, contando posicoes
    /// diferentes como diferentes.
    ///
    /// Cada centro com raio r contribui com teto(r/2) palindromos daquela
    /// paridade, e somar isso sobre os centros conta todos sem repetir.
    /// </summary>
    public static long Quantos(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        return Raios(texto).Sum(r => (long)((r + 1) / 2));
    }

    /// <summary>
    /// Verdadeiro quando a faixa [inicio, fim) do texto e um palindromo,
    /// respondido em TEMPO CONSTANTE a partir dos raios.
    ///
    /// E o que a estrutura compra: depois de uma passada linear, qualquer
    /// pergunta sobre qualquer faixa custa uma comparacao.
    /// </summary>
    public static bool EhPalindromo(int[] raios, int inicio, int fim)
    {
        ArgumentNullException.ThrowIfNull(raios);
        if (inicio < 0 || fim > (raios.Length - 1) / 2 || inicio > fim)
            throw new ArgumentOutOfRangeException(nameof(inicio));
        if (fim - inicio <= 1) return true;
        var centro = inicio + fim;          // o centro no texto intercalado
        return raios[centro] >= fim - inicio;
    }

    /// <summary>O maior palindromo por forca bruta, testando cada faixa.</summary>
    public static string MaiorPorForcaBruta(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        var melhor = string.Empty;
        for (var i = 0; i < texto.Length; i++)
            for (var j = i + 1; j <= texto.Length; j++)
            {
                if (j - i <= melhor.Length) continue;
                var pedaco = texto[i..j];
                if (Simetrico(pedaco)) melhor = pedaco;
            }
        return melhor;
    }

    /// <summary>Quantos palindromos, por forca bruta.</summary>
    public static long QuantosPorForcaBruta(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        long quantos = 0;
        for (var i = 0; i < texto.Length; i++)
            for (var j = i + 1; j <= texto.Length; j++)
                if (Simetrico(texto[i..j])) quantos++;
        return quantos;
    }

    private static bool Simetrico(string cadeia)
    {
        for (int e = 0, d = cadeia.Length - 1; e < d; e++, d--)
            if (cadeia[e] != cadeia[d]) return false;
        return true;
    }
}
