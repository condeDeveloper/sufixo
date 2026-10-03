namespace Conde.Sufixo;

/// <summary>
/// A transformada de Burrows e Wheeler, 1994: a permutacao que faz o bzip2
/// existir.
///
/// Ela nao comprime nada. O que ela faz e REORDENAR os caracteres de um jeito
/// que agrupa os iguais, e a compressao vem depois, de qualquer metodo que
/// goste de repeticao. Em "banana" a transformada da "nnbaaa": de seis blocos
/// de caracteres iguais para tres.
///
/// O exemplo que aparece em quase todo material e "annb$aa", e ele e de outra
/// coisa: da transformada de "banana$", com a sentinela que o indice FM
/// precisa. Sem sentinela, a matriz de rotacoes e outra e a resposta tambem. Eu
/// escrevi "annbaa" aqui de cabeca e o teste contra a matriz me corrigiu.
///
/// O motivo e o seguinte: a transformada e a ultima coluna da matriz de todas
/// as rotacoes ordenadas. Como as linhas estao ordenadas pelo que vem DEPOIS, os
/// caracteres que precedem contextos parecidos acabam vizinhos. Em texto de
/// verdade, "que" quase sempre vem depois de espaco ou de virgula, entao a
/// coluna junta esses.
///
/// A parte espantosa e que ela e INVERTIVEL, e sem guardar a matriz. O unico
/// extra e o indice da linha original. A inversao se apoia numa propriedade que
/// parece magica e e so ordenacao estavel: a k-esima ocorrencia de um caractere
/// na ultima coluna e a MESMA ocorrencia dele na primeira.
///
/// Aqui a transformada sai da ordem das ROTACOES calculada por dobra, o que e
/// muito mais barato que montar a matriz: a ultima coluna da linha i e o
/// caractere ANTES do inicio dela.
/// </summary>
public static class Bwt
{
    /// <summary>
    /// A transformada, mais o indice da linha que corresponde ao texto
    /// original.
    /// </summary>
    public static (string Texto, int Linha) Transformar(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        if (texto.Length == 0) return (string.Empty, 0);

        // A ordem das ROTACOES, e nao o vetor de sufixos. A diferenca nao e
        // detalhe: com o vetor de sufixos, "banana" da "nnbaaa", que ainda e
        // invertivel e NAO e a transformada publicada. A matriz de rotacoes e a
        // definicao, e e ela que agrupa os iguais do jeito que o bzip2 espera.
        var rotacoes = Vetor.OrdemDasRotacoes(texto, out _);
        var saida = new char[texto.Length];
        var linha = 0;

        for (var i = 0; i < rotacoes.Length; i++)
        {
            // A ultima coluna da linha i e o caractere ANTES do inicio dela,
            // com volta circular.
            var antes = rotacoes[i] == 0 ? texto.Length - 1 : rotacoes[i] - 1;
            saida[i] = texto[antes];
            if (rotacoes[i] == 0) linha = i;
        }
        return (new string(saida), linha);
    }

    /// <summary>
    /// A inversa, pelo VETOR DE PROXIMOS.
    ///
    /// A primeira coluna e a ultima ordenada, e nao precisa ser guardada. O
    /// vetor de proximos liga cada posicao da ultima coluna a posicao da mesma
    /// ocorrencia na primeira, e seguir essa ligacao a partir da linha original
    /// reconstroi o texto de tras para frente.
    ///
    /// A ordenacao da primeira coluna precisa ser ESTAVEL para que "a k-esima
    /// ocorrencia" seja a mesma dos dois lados. Aqui ela e por contagem, que e
    /// estavel por construcao.
    /// </summary>
    public static string Inverter(string transformada, int linha)
    {
        ArgumentNullException.ThrowIfNull(transformada);
        var n = transformada.Length;
        if (n == 0) return string.Empty;
        if (linha < 0 || linha >= n) throw new ArgumentOutOfRangeException(nameof(linha));

        // Quantos de cada caractere, e onde cada um comeca na primeira coluna.
        var contagem = new int[char.MaxValue + 1];
        foreach (var letra in transformada) contagem[letra]++;
        var comeco = new int[char.MaxValue + 1];
        var acumulado = 0;
        for (var letra = 0; letra <= char.MaxValue; letra++)
        {
            comeco[letra] = acumulado;
            acumulado += contagem[letra];
        }

        // proximo[i] e a linha que vem depois desta na leitura.
        var proximo = new int[n];
        var usados = new int[char.MaxValue + 1];
        for (var i = 0; i < n; i++)
        {
            var letra = transformada[i];
            proximo[comeco[letra] + usados[letra]] = i;
            usados[letra]++;
        }

        var saida = new char[n];
        var atual = proximo[linha];
        for (var i = 0; i < n; i++)
        {
            saida[i] = transformada[atual];
            atual = proximo[atual];
        }
        return new string(saida);
    }

    /// <summary>
    /// A transformada pela DEFINICAO: monta a matriz de rotacoes, ordena e pega
    /// a ultima coluna.
    ///
    /// Custa O(n² log n) em tempo e O(n²) em memoria, e e o juiz.
    /// </summary>
    public static (string Texto, int Linha) PorForcaBruta(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        if (texto.Length == 0) return (string.Empty, 0);

        var rotacoes = new List<(string Cadeia, int Origem)>(texto.Length);
        for (var i = 0; i < texto.Length; i++)
            rotacoes.Add((texto[i..] + texto[..i], i));
        rotacoes.Sort((a, b) =>
        {
            var ordem = string.CompareOrdinal(a.Cadeia, b.Cadeia);
            return ordem != 0 ? ordem : a.Origem.CompareTo(b.Origem);
        });

        var saida = new char[texto.Length];
        var linha = 0;
        for (var i = 0; i < rotacoes.Count; i++)
        {
            saida[i] = rotacoes[i].Cadeia[^1];
            if (rotacoes[i].Origem == 0) linha = i;
        }
        return (new string(saida), linha);
    }

    /// <summary>
    /// Quanto a transformada AGRUPA os iguais, medido como a quantidade de
    /// blocos de caracteres repetidos.
    ///
    /// E o numero que explica o bzip2: quanto menos blocos, mais a codificacao
    /// por repeticao tem o que comprimir. Em texto de verdade a queda e grande;
    /// em texto sorteado nao ha o que agrupar, e a transformada nao ajuda em
    /// nada. O medidor mostra os dois.
    /// </summary>
    public static int Blocos(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        if (texto.Length == 0) return 0;
        var quantos = 1;
        for (var i = 1; i < texto.Length; i++)
            if (texto[i] != texto[i - 1]) quantos++;
        return quantos;
    }
}
