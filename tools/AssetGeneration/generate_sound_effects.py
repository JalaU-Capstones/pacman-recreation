#!/usr/bin/env python3
"""
Generador de Efectos de Sonido - Estilo Arcade 8-bit
Genera todos los SFX necesarios para el juego Pac-Man
"""

import numpy as np
import wave as wave_module
import struct
import math

# Configuración de audio
SAMPLE_RATE = 44100  # 44.1 kHz (calidad CD)
BITS_PER_SAMPLE = 16

def generate_sine_wave(frequency, duration, sample_rate=SAMPLE_RATE):
    """
    Genera una onda sinusoidal pura
    
    Args:
        frequency: Frecuencia en Hz
        duration: Duración en segundos
        sample_rate: Tasa de muestreo
    
    Returns:
        numpy array con la onda
    """
    num_samples = int(sample_rate * duration)
    t = np.linspace(0, duration, num_samples, False)
    wave = np.sin(2 * np.pi * frequency * t)
    return wave

def generate_square_wave(frequency, duration, sample_rate=SAMPLE_RATE):
    """
    Genera una onda cuadrada (sonido más retro/8-bit)
    """
    num_samples = int(sample_rate * duration)
    t = np.linspace(0, duration, num_samples, False)
    wave = np.sign(np.sin(2 * np.pi * frequency * t))
    return wave

def generate_sawtooth_wave(frequency, duration, sample_rate=SAMPLE_RATE):
    """
    Genera una onda de sierra
    """
    num_samples = int(sample_rate * duration)
    t = np.linspace(0, duration, num_samples, False)
    wave = 2 * (t * frequency - np.floor(t * frequency + 0.5))
    return wave

def generate_triangle_wave(frequency, duration, sample_rate=SAMPLE_RATE):
    """
    Genera una onda triangular
    """
    num_samples = int(sample_rate * duration)
    t = np.linspace(0, duration, num_samples, False)
    wave = 2 * np.abs(2 * (t * frequency - np.floor(t * frequency + 0.5))) - 1
    return wave

def apply_envelope(wave, attack=0.01, decay=0.05, sustain_level=0.7, release=0.1):
    """
    Aplica un envelope ADSR (Attack, Decay, Sustain, Release) a la onda
    """
    num_samples = len(wave)
    envelope = np.ones(num_samples)
    
    attack_samples = int(attack * SAMPLE_RATE)
    decay_samples = int(decay * SAMPLE_RATE)
    release_samples = int(release * SAMPLE_RATE)
    
    # Attack
    if attack_samples > 0:
        envelope[:attack_samples] = np.linspace(0, 1, attack_samples)
    
    # Decay
    if decay_samples > 0:
        decay_start = attack_samples
        decay_end = attack_samples + decay_samples
        envelope[decay_start:decay_end] = np.linspace(1, sustain_level, decay_samples)
    
    # Sustain (ya está en sustain_level)
    sustain_start = attack_samples + decay_samples
    sustain_end = num_samples - release_samples
    if sustain_end > sustain_start:
        envelope[sustain_start:sustain_end] = sustain_level
    
    # Release
    if release_samples > 0 and num_samples > release_samples:
        envelope[-release_samples:] = np.linspace(sustain_level, 0, release_samples)
    
    return wave * envelope

def add_noise(wave, noise_level=0.02):
    """
    Añade ruido blanco para textura más orgánica
    """
    noise = np.random.normal(0, noise_level, len(wave))
    return wave + noise

def normalize_wave(wave):
    """
    Normaliza la onda para evitar clipping y usar el rango completo
    """
    max_val = np.max(np.abs(wave))
    if max_val > 0:
        wave = wave / max_val
    return wave * 0.9  # Dejar un poco de headroom

def save_wav(filename, wave, sample_rate=SAMPLE_RATE):
    """
    Guarda la onda como archivo WAV
    """
    # Normalizar y convertir a 16-bit
    wave = normalize_wave(wave)
    wave_int = np.int16(wave * 32767)
    
    # Crear archivo WAV
    with wave_module.open(filename, 'w') as wav_file:
        # Configurar parámetros: mono, 16-bit, sample_rate
        wav_file.setnchannels(1)
        wav_file.setsampwidth(2)  # 2 bytes = 16 bits
        wav_file.setframerate(sample_rate)
        
        # Escribir datos
        wav_file.writeframes(wave_int.tobytes())
    
    print(f"✅ Guardado: {filename}")

# ============================================
# EFECTOS DE SONIDO ESPECÍFICOS
# ============================================

def create_chomp_sound():
    """
    Sonido de Pac-Man comiendo puntos pequeños
    Sonido muy corto y agudo tipo "waka"
    """
    duration = 0.08
    
    # Dos tonos rápidos que bajan
    freq1 = 800
    freq2 = 400
    
    half_duration = duration / 2
    
    # Primera mitad (tono alto)
    wave1 = generate_square_wave(freq1, half_duration)
    wave1 = apply_envelope(wave1, attack=0.001, decay=0.01, sustain_level=0.6, release=0.02)
    
    # Segunda mitad (tono bajo)
    wave2 = generate_square_wave(freq2, half_duration)
    wave2 = apply_envelope(wave2, attack=0.001, decay=0.01, sustain_level=0.5, release=0.02)
    
    # Combinar
    wave = np.concatenate([wave1, wave2])
    
    return wave

def create_eat_power_pellet_sound():
    """
    Sonido de comer power pellet (punto grande)
    Más largo y dramático que el chomp normal
    """
    duration = 0.3
    
    # Secuencia de tonos ascendentes
    freqs = [400, 500, 600, 800]
    waves = []
    
    segment_duration = duration / len(freqs)
    
    for freq in freqs:
        segment = generate_square_wave(freq, segment_duration)
        segment = apply_envelope(segment, attack=0.005, decay=0.02, sustain_level=0.7, release=0.03)
        waves.append(segment)
    
    wave = np.concatenate(waves)
    
    return wave

def create_eat_ghost_sound():
    """
    Sonido de comer fantasma vulnerable
    Tono ascendente rápido y satisfactorio
    """
    duration = 0.4
    
    # Sweep de frecuencia ascendente
    num_samples = int(SAMPLE_RATE * duration)
    t = np.linspace(0, duration, num_samples, False)
    
    # Frecuencia que sube de 200 Hz a 800 Hz
    freq_start = 200
    freq_end = 800
    instantaneous_freq = freq_start + (freq_end - freq_start) * (t / duration)
    
    # Generar onda con frecuencia variable
    phase = 2 * np.pi * np.cumsum(instantaneous_freq) / SAMPLE_RATE
    wave = np.sin(phase)
    
    wave = apply_envelope(wave, attack=0.01, decay=0.05, sustain_level=0.8, release=0.1)
    
    return wave

def create_eat_fruit_sound():
    """
    Sonido de comer fruta
    Melodía corta y alegre
    """
    duration = 0.5
    
    # Secuencia melódica
    notes = [
        (659, 0.1),  # E5
        (784, 0.1),  # G5
        (988, 0.15), # B5
        (1319, 0.15) # E6
    ]
    
    waves = []
    for freq, note_duration in notes:
        segment = generate_sine_wave(freq, note_duration)
        segment = apply_envelope(segment, attack=0.005, decay=0.02, sustain_level=0.7, release=0.03)
        waves.append(segment)
    
    wave = np.concatenate(waves)
    
    return wave

def create_death_sound():
    """
    Sonido de muerte de Pac-Man
    Tono descendente dramático
    """
    duration = 1.0
    
    # Sweep de frecuencia descendente
    num_samples = int(SAMPLE_RATE * duration)
    t = np.linspace(0, duration, num_samples, False)
    
    # Frecuencia que baja de 800 Hz a 100 Hz
    freq_start = 800
    freq_end = 100
    instantaneous_freq = freq_start - (freq_start - freq_end) * (t / duration)
    
    # Generar onda con frecuencia variable
    phase = 2 * np.pi * np.cumsum(instantaneous_freq) / SAMPLE_RATE
    wave = np.sin(phase)
    
    # Envelope que se desvanece gradualmente
    wave = apply_envelope(wave, attack=0.01, decay=0.1, sustain_level=0.6, release=0.4)
    
    return wave

def create_extra_life_sound():
    """
    Sonido de obtener vida extra
    Melodía ascendente alegre
    """
    duration = 0.8
    
    # Arpeggio ascendente
    notes = [
        (523, 0.15),  # C5
        (659, 0.15),  # E5
        (784, 0.15),  # G5
        (1047, 0.2),  # C6
        (1047, 0.15)  # C6 (repetido)
    ]
    
    waves = []
    for freq, note_duration in notes:
        segment = generate_sine_wave(freq, note_duration)
        segment = apply_envelope(segment, attack=0.01, decay=0.03, sustain_level=0.7, release=0.05)
        waves.append(segment)
    
    wave = np.concatenate(waves)
    
    return wave

def create_game_start_sound():
    """
    Sonido de inicio de nivel
    Melodía icónica tipo fanfare
    """
    duration = 2.0
    
    # Melodía de inicio
    notes = [
        (392, 0.15),  # G4
        (523, 0.15),  # C5
        (659, 0.15),  # E5
        (784, 0.2),   # G5
        (659, 0.15),  # E5
        (784, 0.3),   # G5
        (0, 0.2),     # Silencio
        (659, 0.2),   # E5
        (784, 0.4)    # G5
    ]
    
    waves = []
    for freq, note_duration in notes:
        if freq == 0:  # Silencio
            segment = np.zeros(int(SAMPLE_RATE * note_duration))
        else:
            segment = generate_sine_wave(freq, note_duration)
            segment = apply_envelope(segment, attack=0.01, decay=0.03, sustain_level=0.7, release=0.05)
        waves.append(segment)
    
    wave = np.concatenate(waves)
    
    return wave

def create_level_complete_sound():
    """
    Sonido de completar nivel
    Melodía victoriosa
    """
    duration = 1.5
    
    # Secuencia victoriosa
    notes = [
        (659, 0.1),   # E5
        (659, 0.1),   # E5
        (659, 0.2),   # E5 (más largo)
        (523, 0.1),   # C5
        (659, 0.1),   # E5
        (784, 0.3),   # G5
        (392, 0.3)    # G4
    ]
    
    waves = []
    for freq, note_duration in notes:
        segment = generate_triangle_wave(freq, note_duration)
        segment = apply_envelope(segment, attack=0.01, decay=0.02, sustain_level=0.8, release=0.04)
        waves.append(segment)
    
    wave = np.concatenate(waves)
    
    return wave

def create_game_over_sound():
    """
    Sonido de Game Over
    Melodía descendente triste
    """
    duration = 2.0
    
    # Secuencia descendente
    notes = [
        (523, 0.3),   # C5
        (494, 0.3),   # B4
        (440, 0.3),   # A4
        (392, 0.3),   # G4
        (349, 0.4),   # F4
        (330, 0.4)    # E4
    ]
    
    waves = []
    for freq, note_duration in notes:
        segment = generate_sine_wave(freq, note_duration)
        segment = apply_envelope(segment, attack=0.02, decay=0.05, sustain_level=0.6, release=0.1)
        waves.append(segment)
    
    wave = np.concatenate(waves)
    
    return wave

def create_menu_select_sound():
    """
    Sonido de seleccionar opción en menú
    Blip corto y agradable
    """
    duration = 0.1
    
    wave = generate_square_wave(800, duration)
    wave = apply_envelope(wave, attack=0.005, decay=0.02, sustain_level=0.5, release=0.03)
    
    return wave

def create_menu_navigate_sound():
    """
    Sonido de navegar en el menú
    Blip más suave
    """
    duration = 0.08
    
    wave = generate_sine_wave(600, duration)
    wave = apply_envelope(wave, attack=0.005, decay=0.015, sustain_level=0.4, release=0.02)
    
    return wave

def create_ghost_return_sound():
    """
    Sonido de fantasma regresando a la base (opcional)
    Tono rápido ascendente
    """
    duration = 0.3
    
    # Sweep rápido
    num_samples = int(SAMPLE_RATE * duration)
    t = np.linspace(0, duration, num_samples, False)
    
    freq_start = 300
    freq_end = 600
    instantaneous_freq = freq_start + (freq_end - freq_start) * (t / duration)
    
    phase = 2 * np.pi * np.cumsum(instantaneous_freq) / SAMPLE_RATE
    wave = np.sin(phase)
    
    wave = apply_envelope(wave, attack=0.01, decay=0.05, sustain_level=0.5, release=0.08)
    
    return wave

# ============================================
# FUNCIÓN PRINCIPAL
# ============================================

def main():
    print("🔊 Generador de Efectos de Sonido Arcade")
    print("=" * 50)
    print("Generando efectos de sonido estilo 8-bit...")
    print()

    
    # Diccionario de efectos de sonido
    sound_effects = {
        "chomp.wav": create_chomp_sound,
        "eat-power-pellet.wav": create_eat_power_pellet_sound,
        "eat-ghost.wav": create_eat_ghost_sound,
        "eat-fruit.wav": create_eat_fruit_sound,
        "death.wav": create_death_sound,
        "extra-life.wav": create_extra_life_sound,
        "game-start.wav": create_game_start_sound,
        "level-complete.wav": create_level_complete_sound,
        "game-over.wav": create_game_over_sound,
        "menu-select.wav": create_menu_select_sound,
        "menu-navigate.wav": create_menu_navigate_sound,
        "ghost-return.wav": create_ghost_return_sound
    }
    
    # Generar todos los efectos
    print("📦 Generando archivos WAV:")
    print()
    
    for filename, generator_func in sound_effects.items():
        wave = generator_func()
        save_wav(filename, wave)
    
    # Resumen
    print()
    print("=" * 50)
    print("✨ ¡Generación completada!")
    print()
    print(f"📊 Total de efectos generados: {len(sound_effects)}")
    print()
    print("📁 Efectos de sonido creados:")
    print("   Pac-Man:")
    print("   - chomp.wav (comer punto pequeño)")
    print("   - eat-power-pellet.wav (comer power pellet)")
    print("   - eat-ghost.wav (comer fantasma)")
    print("   - eat-fruit.wav (comer fruta)")
    print("   - death.wav (muerte)")
    print("   - extra-life.wav (vida extra)")
    print()
    print("   Sistema:")
    print("   - game-start.wav (inicio de nivel)")
    print("   - level-complete.wav (nivel completado)")
    print("   - game-over.wav (fin del juego)")
    print()
    print("   UI/Menú:")
    print("   - menu-select.wav (seleccionar opción)")
    print("   - menu-navigate.wav (navegar menú)")
    print()
    print("   Fantasmas:")
    print("   - ghost-return.wav (fantasma regresando)")
    print()
    print("💡 Características:")
    print("   - Formato: WAV (44.1 kHz, 16-bit, mono)")
    print("   - Estilo: Arcade/8-bit retro")
    print("   - Síntesis: Ondas cuadradas, sinusoidales y triangulares")
    print("   - Optimizados para juegos")
    print()
    print("🎮 ¡Listos para usar en tu juego Pac-Man!")

if __name__ == "__main__":
    main()
