#!/usr/bin/env python3
"""
Generador de Música de Fondo - Estilo Arcade/Chiptune
Genera 3 pistas musicales completas para el juego Pac-Man
"""

import numpy as np
import wave as wave_module
import struct
import math

# Configuración de audio
SAMPLE_RATE = 44100  # 44.1 kHz
BPM = 140  # Beats por minuto (tempo arcade energético)
BEAT_DURATION = 60.0 / BPM  # Duración de un beat en segundos

# Notas musicales en Hz (escala cromática)
NOTES = {
    'C3': 130.81, 'C#3': 138.59, 'D3': 146.83, 'D#3': 155.56, 'E3': 164.81, 'F3': 174.61,
    'F#3': 185.00, 'G3': 196.00, 'G#3': 207.65, 'A3': 220.00, 'A#3': 233.08, 'B3': 246.94,
    'C4': 261.63, 'C#4': 277.18, 'D4': 293.66, 'D#4': 311.13, 'E4': 329.63, 'F4': 349.23,
    'F#4': 369.99, 'G4': 392.00, 'G#4': 415.30, 'A4': 440.00, 'A#4': 466.16, 'B4': 493.88,
    'C5': 523.25, 'C#5': 554.37, 'D5': 587.33, 'D#5': 622.25, 'E5': 659.25, 'F5': 698.46,
    'F#5': 739.99, 'G5': 783.99, 'G#5': 830.61, 'A5': 880.00, 'A#5': 932.33, 'B5': 987.77,
    'C6': 1046.50, 'REST': 0
}

def generate_square_wave(frequency, duration, sample_rate=SAMPLE_RATE, duty_cycle=0.5):
    """Genera una onda cuadrada (sonido 8-bit clásico)"""
    if frequency == 0:  # Silencio
        return np.zeros(int(sample_rate * duration))
    
    num_samples = int(sample_rate * duration)
    t = np.linspace(0, duration, num_samples, False)
    wave = np.sign(np.sin(2 * np.pi * frequency * t))
    
    # Aplicar duty cycle
    wave[wave > 0] = 1
    wave[wave <= 0] = -duty_cycle
    
    return wave

def generate_triangle_wave(frequency, duration, sample_rate=SAMPLE_RATE):
    """Genera una onda triangular (sonido más suave)"""
    if frequency == 0:
        return np.zeros(int(sample_rate * duration))
    
    num_samples = int(sample_rate * duration)
    t = np.linspace(0, duration, num_samples, False)
    wave = 2 * np.abs(2 * (t * frequency - np.floor(t * frequency + 0.5))) - 1
    return wave

def generate_pulse_wave(frequency, duration, sample_rate=SAMPLE_RATE):
    """Genera una onda de pulso (25% duty cycle para bajo)"""
    return generate_square_wave(frequency, duration, sample_rate, duty_cycle=0.25)

def apply_adsr(wave, attack=0.01, decay=0.05, sustain_level=0.7, release=0.05):
    """Aplica envelope ADSR a la onda"""
    num_samples = len(wave)
    envelope = np.ones(num_samples)
    
    attack_samples = int(attack * SAMPLE_RATE)
    decay_samples = int(decay * SAMPLE_RATE)
    release_samples = int(release * SAMPLE_RATE)
    
    # Attack
    if attack_samples > 0 and attack_samples < num_samples:
        envelope[:attack_samples] = np.linspace(0, 1, attack_samples)
    
    # Decay
    if decay_samples > 0:
        decay_start = attack_samples
        decay_end = min(attack_samples + decay_samples, num_samples)
        if decay_end > decay_start:
            envelope[decay_start:decay_end] = np.linspace(1, sustain_level, decay_end - decay_start)
    
    # Sustain
    sustain_start = attack_samples + decay_samples
    sustain_end = num_samples - release_samples
    if sustain_end > sustain_start:
        envelope[sustain_start:sustain_end] = sustain_level
    
    # Release
    if release_samples > 0 and num_samples > release_samples:
        envelope[-release_samples:] = np.linspace(sustain_level, 0, release_samples)
    
    return wave * envelope

def create_note(note_name, duration, wave_type='square', volume=0.5):
    """Crea una nota musical con el tipo de onda especificado"""
    frequency = NOTES.get(note_name, 0)
    
    if wave_type == 'square':
        wave = generate_square_wave(frequency, duration)
    elif wave_type == 'triangle':
        wave = generate_triangle_wave(frequency, duration)
    elif wave_type == 'pulse':
        wave = generate_pulse_wave(frequency, duration)
    else:
        wave = generate_square_wave(frequency, duration)
    
    # Aplicar ADSR
    if frequency > 0:
        wave = apply_adsr(wave, attack=0.01, decay=0.05, sustain_level=0.7, release=0.05)
    
    return wave * volume

def create_arpeggio(notes, duration, wave_type='square', volume=0.5):
    """Crea un arpegio rápido con múltiples notas"""
    note_duration = duration / len(notes)
    waves = []
    
    for note in notes:
        note_wave = create_note(note, note_duration, wave_type, volume)
        waves.append(note_wave)
    
    return np.concatenate(waves)

def mix_tracks(*tracks):
    """Mezcla múltiples pistas de audio"""
    # Encontrar la longitud máxima
    max_length = max(len(track) for track in tracks)
    
    # Extender todas las pistas a la misma longitud
    extended_tracks = []
    for track in tracks:
        if len(track) < max_length:
            padding = np.zeros(max_length - len(track))
            track = np.concatenate([track, padding])
        extended_tracks.append(track)
    
    # Mezclar sumando todas las pistas
    mixed = np.sum(extended_tracks, axis=0)
    
    # Normalizar para evitar clipping
    max_val = np.max(np.abs(mixed))
    if max_val > 0:
        mixed = mixed / max_val * 0.8  # 80% del máximo para headroom
    
    return mixed

def normalize_wave(wave):
    """Normaliza la onda"""
    max_val = np.max(np.abs(wave))
    if max_val > 0:
        wave = wave / max_val
    return wave * 0.9

def save_wav(filename, wave, sample_rate=SAMPLE_RATE):
    """Guarda la onda como archivo WAV"""
    wave = normalize_wave(wave)
    wave_int = np.int16(wave * 32767)
    
    with wave_module.open(filename, 'w') as wav_file:
        wav_file.setnchannels(1)
        wav_file.setsampwidth(2)
        wav_file.setframerate(sample_rate)
        wav_file.writeframes(wave_int.tobytes())
    
    print(f"✅ Guardado: {filename}")

# ============================================
# COMPOSICIONES MUSICALES
# ============================================

def create_main_theme():
    """
    Tema principal del juego - Energético y pegajoso
    Inspirado en el estilo arcade clásico con melodía memorable
    """
    print("🎵 Componiendo tema principal...")
    
    # Duración de cada nota (en beats)
    eighth = BEAT_DURATION / 2      # Corchea
    quarter = BEAT_DURATION          # Negra
    half = BEAT_DURATION * 2         # Blanca
    
    # MELODÍA PRINCIPAL (Canal 1 - Lead)
    melody_pattern = [
        # Frase 1 (pegajosa y memorable)
        ('E5', eighth), ('E5', eighth), ('E5', eighth), ('C5', eighth),
        ('E5', quarter), ('G5', quarter), ('REST', quarter), ('G4', quarter),
        
        # Frase 2
        ('C5', eighth), ('REST', eighth), ('G4', eighth), ('REST', eighth),
        ('E4', eighth), ('REST', eighth), ('A4', eighth), ('B4', eighth),
        ('A#4', eighth), ('A4', eighth),
        
        # Frase 3 (repetir variación)
        ('G4', eighth), ('E5', eighth), ('G5', eighth), ('A5', quarter),
        ('F5', eighth), ('G5', eighth), ('REST', eighth), ('E5', eighth),
        ('C5', eighth), ('D5', eighth), ('B4', quarter),
    ]
    
    melody_waves = []
    for note, duration in melody_pattern:
        melody_waves.append(create_note(note, duration, 'square', volume=0.6))
    melody = np.concatenate(melody_waves)
    
    # Repetir melodía para hacer el tema más largo
    melody = np.tile(melody, 4)  # 4 repeticiones
    
    # BAJO (Canal 2 - Bass)
    bass_pattern = [
        ('C3', quarter), ('C3', quarter), ('C3', quarter), ('C3', quarter),
        ('G3', quarter), ('G3', quarter), ('G3', quarter), ('G3', quarter),
        ('A3', quarter), ('A3', quarter), ('F3', quarter), ('F3', quarter),
        ('G3', quarter), ('G3', quarter), ('C3', quarter), ('C3', quarter),
    ]
    
    bass_waves = []
    for note, duration in bass_pattern:
        bass_waves.append(create_note(note, duration, 'pulse', volume=0.4))
    bass = np.concatenate(bass_waves)
    bass = np.tile(bass, 4)
    
    # ARMONÍA (Canal 3 - Harmony)
    harmony_pattern = [
        ('G4', half), ('E4', half),
        ('C5', half), ('G4', half),
        ('F4', half), ('D4', half),
        ('E4', half), ('C4', half),
    ]
    
    harmony_waves = []
    for note, duration in harmony_pattern:
        harmony_waves.append(create_note(note, duration, 'triangle', volume=0.3))
    harmony = np.concatenate(harmony_waves)
    harmony = np.tile(harmony, 4)
    
    # PERCUSIÓN (simulada con ruido)
    # Crear patrón de kick y hi-hat
    beat_duration = quarter
    num_beats = int(len(melody) / (SAMPLE_RATE * beat_duration))
    percussion = np.zeros(len(melody))
    
    for i in range(num_beats):
        # Kick en cada beat
        kick_pos = int(i * SAMPLE_RATE * beat_duration)
        kick_length = int(SAMPLE_RATE * 0.05)
        if kick_pos + kick_length < len(percussion):
            kick = np.random.uniform(-0.3, 0.3, kick_length)
            kick = kick * np.linspace(1, 0, kick_length)
            percussion[kick_pos:kick_pos + kick_length] += kick
    
    # Mezclar todos los canales
    theme = mix_tracks(melody, bass, harmony, percussion)
    
    return theme

def create_menu_theme():
    """
    Tema del menú - Más tranquilo pero aún retro
    Melodía simple y relajante para no distraer
    """
    print("🎵 Componiendo tema del menú...")
    
    eighth = BEAT_DURATION / 2
    quarter = BEAT_DURATION
    half = BEAT_DURATION * 2
    
    # MELODÍA PRINCIPAL (más lenta y espaciada)
    melody_pattern = [
        ('C5', quarter), ('E5', quarter), ('G5', quarter), ('E5', quarter),
        ('F5', quarter), ('D5', quarter), ('G5', half),
        
        ('C5', quarter), ('E5', quarter), ('G5', quarter), ('C6', quarter),
        ('B5', quarter), ('A5', quarter), ('G5', half),
        
        ('A5', quarter), ('G5', quarter), ('F5', quarter), ('E5', quarter),
        ('D5', quarter), ('E5', quarter), ('C5', half),
        
        ('E5', quarter), ('D5', quarter), ('C5', quarter), ('D5', quarter),
        ('E5', quarter), ('G5', quarter), ('C5', half),
    ]
    
    melody_waves = []
    for note, duration in melody_pattern:
        melody_waves.append(create_note(note, duration, 'triangle', volume=0.5))
    melody = np.concatenate(melody_waves)
    melody = np.tile(melody, 3)  # 3 repeticiones
    
    # BAJO (Canal 2)
    bass_pattern = [
        ('C3', half), ('C3', half),
        ('F3', half), ('G3', half),
        ('C3', half), ('C3', half),
        ('G3', half), ('C3', half),
    ]
    
    bass_waves = []
    for note, duration in bass_pattern:
        bass_waves.append(create_note(note, duration, 'pulse', volume=0.35))
    bass = np.concatenate(bass_waves)
    bass = np.tile(bass, 3)
    
    # ARPEGIO DE FONDO (Canal 3)
    arp_notes = ['C4', 'E4', 'G4', 'C5']
    arp_duration = quarter
    
    arp_waves = []
    for _ in range(int(len(melody) / (SAMPLE_RATE * arp_duration))):
        arp = create_arpeggio(arp_notes, arp_duration, 'square', volume=0.2)
        arp_waves.append(arp)
    
    if arp_waves:
        arpeggios = np.concatenate(arp_waves)
        # Ajustar longitud
        if len(arpeggios) > len(melody):
            arpeggios = arpeggios[:len(melody)]
        elif len(arpeggios) < len(melody):
            padding = np.zeros(len(melody) - len(arpeggios))
            arpeggios = np.concatenate([arpeggios, padding])
    else:
        arpeggios = np.zeros(len(melody))
    
    # Mezclar
    theme = mix_tracks(melody, bass, arpeggios)
    
    return theme

def create_game_over_theme():
    """
    Tema de Game Over - Melancólico y descendente
    Melodía triste que indica el fin del juego
    """
    print("🎵 Componiendo tema de Game Over...")
    
    quarter = BEAT_DURATION
    half = BEAT_DURATION * 2
    whole = BEAT_DURATION * 4
    
    # MELODÍA PRINCIPAL (descendente y triste)
    melody_pattern = [
        ('C5', quarter), ('B4', quarter), ('A#4', quarter), ('A4', quarter),
        ('G#4', quarter), ('G4', quarter), ('F#4', quarter), ('F4', quarter),
        
        ('E4', half), ('D4', half),
        ('C4', half), ('REST', half),
        
        ('A4', quarter), ('G4', quarter), ('F4', quarter), ('E4', quarter),
        ('D4', quarter), ('C4', quarter), ('B3', half),
        
        ('C4', whole),
        ('REST', quarter),
    ]
    
    melody_waves = []
    for note, duration in melody_pattern:
        melody_waves.append(create_note(note, duration, 'triangle', volume=0.6))
    melody = np.concatenate(melody_waves)
    melody = np.tile(melody, 2)  # 2 repeticiones
    
    # BAJO (Canal 2 - notas largas y profundas)
    bass_pattern = [
        ('C3', whole), ('F3', whole),
        ('G3', whole), ('C3', whole),
        ('A2', whole), ('D3', whole),
        ('G2', whole), ('C2', whole),
    ]
    
    bass_waves = []
    for note, duration in bass_pattern:
        bass_waves.append(create_note(note, duration, 'pulse', volume=0.4))
    bass = np.concatenate(bass_waves)
    bass = np.tile(bass, 2)
    
    # PAD (Canal 3 - acordes sostenidos)
    pad_pattern = [
        ('E4', whole), ('F4', whole),
        ('D4', whole), ('C4', whole),
    ]
    
    pad_waves = []
    for note, duration in pad_pattern:
        pad_waves.append(create_note(note, duration, 'triangle', volume=0.25))
    pad = np.concatenate(pad_waves)
    pad = np.tile(pad, 2)
    
    # Mezclar
    theme = mix_tracks(melody, bass, pad)
    
    return theme

# ============================================
# FUNCIÓN PRINCIPAL
# ============================================

def main():
    print("🎼 Generador de Música de Fondo Arcade")
    print("=" * 50)
    print("Componiendo música estilo chiptune/8-bit...")
    print()
    
    
    # Generar temas musicales
    print("🎹 Generando composiciones:")
    print()
    
    # 1. Tema Principal
    main_theme = create_main_theme()
    save_wav("background-theme.wav", main_theme)
    duration_main = len(main_theme) / SAMPLE_RATE
    print(f"   Duración: {duration_main:.1f} segundos")
    print()
    
    # 2. Tema del Menú
    menu_theme = create_menu_theme()
    save_wav("menu-theme.wav", menu_theme)
    duration_menu = len(menu_theme) / SAMPLE_RATE
    print(f"   Duración: {duration_menu:.1f} segundos")
    print()
    
    # 3. Tema de Game Over
    gameover_theme = create_game_over_theme()
    save_wav("game-over-theme.wav", gameover_theme)
    duration_gameover = len(gameover_theme) / SAMPLE_RATE
    print(f"   Duración: {duration_gameover:.1f} segundos")
    print()
    
    # Resumen
    print("=" * 50)
    print("✨ ¡Composición completada!")
    print()
    print("📁 Música generada:")
    print("   1. background-theme.wav - Tema principal energético")
    print("      └─ Melodía pegajosa estilo arcade")
    print("      └─ 4 canales: Melodía + Bajo + Armonía + Percusión")
    print(f"      └─ {duration_main:.1f}s de loop perfecto")
    print()
    print("   2. menu-theme.wav - Tema del menú tranquilo")
    print("      └─ Melodía relajante pero retro")
    print("      └─ 3 canales: Melodía + Bajo + Arpegios")
    print(f"      └─ {duration_menu:.1f}s de loop suave")
    print()
    print("   3. game-over-theme.wav - Tema melancólico")
    print("      └─ Melodía descendente y triste")
    print("      └─ 3 canales: Melodía + Bajo + Pad")
    print(f"      └─ {duration_gameover:.1f}s de despedida")
    print()
    print("💡 Características:")
    print("   - Formato: WAV (44.1 kHz, 16-bit, mono)")
    print("   - Estilo: Chiptune/Arcade 8-bit auténtico")
    print("   - BPM: 140 (tempo arcade energético)")
    print("   - Síntesis: Ondas cuadradas, triangulares y pulso")
    print("   - Canales múltiples mezclados profesionalmente")
    print("   - Loops perfectos para repetición continua")
    print()
    print("🎮 ¡Listas para darle vida a tu juego Pac-Man!")

if __name__ == "__main__":
    main()
