using System;

namespace MonstersAndSoldiers.Core {
    /// <summary>Estados de uma partida (GDD 10.1).</summary>
    public enum MatchState {
        Playing,
        Paused,
        Attacking,
        Won
    }

    /// <summary>
    /// Tempo, tentativas e estado de uma partida, sem Unity: o GameManager chama Tick a cada frame
    /// e o tempo só corre enquanto o jogador está jogando (GDD 8.4: a pausa para o tempo).
    /// </summary>
    public class MatchSession {
        public MatchState State { get; private set; } = MatchState.Playing;
        public double ElapsedSeconds { get; private set; }
        public int Attempts { get; private set; }

        /// <summary>Estado de antes da pausa, para Continuar voltar para ele.</summary>
        private MatchState stateBeforePause = MatchState.Playing;

        public event Action<MatchState> StateChanged;

        public bool IsPaused => State == MatchState.Paused;

        /// <summary>Recomeça do zero (novo mapa ou Reiniciar).</summary>
        public void Reset() {
            ElapsedSeconds = 0;
            Attempts = 0;
            stateBeforePause = MatchState.Playing;
            SetState(MatchState.Playing);
        }

        public void Tick(double deltaSeconds) {
            if (State == MatchState.Playing && deltaSeconds > 0) ElapsedSeconds += deltaSeconds;
        }

        /// <summary>Registra uma tentativa de ataque. Só vale durante o jogo.</summary>
        public bool BeginAttack() {
            if (State != MatchState.Playing) return false;
            Attempts++;
            SetState(MatchState.Attacking);
            return true;
        }

        /// <summary>Fim da animação de ataque: vitória ou volta a jogar.</summary>
        public void EndAttack(bool success) {
            if (State == MatchState.Paused) {
                stateBeforePause = success ? MatchState.Won : MatchState.Playing;
                return;
            }
            if (State != MatchState.Attacking) return;
            SetState(success ? MatchState.Won : MatchState.Playing);
        }

        public bool Pause() {
            if (State == MatchState.Paused || State == MatchState.Won) return false;
            stateBeforePause = State;
            SetState(MatchState.Paused);
            return true;
        }

        public bool Resume() {
            if (State != MatchState.Paused) return false;
            SetState(stateBeforePause);
            return true;
        }

        /// <summary>Alterna pausa (ESC / Start).</summary>
        public void TogglePause() {
            if (!Pause()) Resume();
        }

        private void SetState(MatchState newState) {
            if (State == newState) return;
            State = newState;
            StateChanged?.Invoke(newState);
        }

        /// <summary>"mm:ss" para o HUD e a vitória.</summary>
        public static string FormatTime(double seconds) {
            int total = (int)Math.Floor(Math.Max(0, seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
