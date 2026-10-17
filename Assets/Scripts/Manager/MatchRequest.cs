using MonstersAndSoldiers.Core;

/// <summary>
/// Modo escolhido no menu, levado para a cena da partida (GDD 8.2).
/// Sem pedido (cena aberta direto no Editor) o LevelManager usa o que está configurado na cena.
/// </summary>
public static class MatchRequest {
    public static bool Pending { get; private set; }
    public static bool Tutorial { get; private set; }
    public static Difficulty Difficulty { get; private set; }

    public static void RequestTutorial() {
        Pending = true;
        Tutorial = true;
    }

    public static void RequestProcedural(Difficulty difficulty) {
        Pending = true;
        Tutorial = false;
        Difficulty = difficulty;
    }

    /// <summary>Lê e limpa o pedido.</summary>
    public static bool TryConsume(out bool tutorial, out Difficulty difficulty) {
        tutorial = Tutorial;
        difficulty = Difficulty;
        if (!Pending) return false;
        Pending = false;
        return true;
    }
}
