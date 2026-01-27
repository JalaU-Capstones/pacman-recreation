#!/usr/bin/env python3
"""
Generador de Sprite Sheet de Fantasmas - Estilo 8-bit Clásico
Genera sprites de Blinky, Pinky, Inky y Clyde con todos sus estados
"""

from PIL import Image, ImageDraw
import math

# Configuración
SPRITE_SIZE = 32  # Tamaño de cada sprite individual

# Colores clásicos de los fantasmas (RGB)
COLORS = {
    'blinky': (255, 0, 0),      # Rojo
    'pinky': (255, 184, 255),   # Rosa
    'inky': (0, 255, 255),      # Cian
    'clyde': (255, 184, 82),    # Naranja
    'vulnerable': (33, 33, 255), # Azul (modo vulnerable)
    'warning': (255, 255, 255),  # Blanco (parpadeo de advertencia)
    'eyes_white': (255, 255, 255), # Blanco de ojos
    'eyes_pupil': (0, 0, 255)    # Pupila azul
}

BACKGROUND = (0, 0, 0, 0)  # Transparente
WHITE = (255, 255, 255)
BLACK = (0, 0, 0)

def create_ghost_body(draw, center, radius, color):
    """
    Dibuja el cuerpo base de un fantasma (parte superior redondeada)
    """
    # Parte superior (semicírculo)
    bbox = [center - radius, center - radius, 
            center + radius, center + radius + 4]
    draw.ellipse(bbox, fill=color, outline=color)
    
    # Parte inferior (rectángulo)
    rect_bbox = [center - radius, center, 
                 center + radius, center + radius + 4]
    draw.rectangle(rect_bbox, fill=color, outline=color)

def create_ghost_wave_bottom(draw, center, radius, color, frame=0):
    """
    Dibuja la parte inferior ondulada del fantasma
    """
    # Número de ondas en la parte inferior
    num_waves = 4
    wave_width = (radius * 2) // num_waves
    
    for i in range(num_waves):
        x_start = center - radius + (i * wave_width)
        x_mid = x_start + wave_width // 2
        x_end = x_start + wave_width
        
        # Alternar altura de las ondas basado en el frame (animación)
        if frame == 0:
            y_bottom = center + radius + 4
        else:
            y_bottom = center + radius + 2 if i % 2 == 0 else center + radius + 6
        
        # Dibujar la onda como un triángulo pequeño
        points = [
            (x_start, center + radius),
            (x_mid, y_bottom),
            (x_end, center + radius)
        ]
        draw.polygon(points, fill=color, outline=color)

def create_ghost_eyes(draw, center, direction='right', is_eyes_only=False):
    """
    Dibuja los ojos del fantasma
    
    Args:
        draw: ImageDraw object
        center: Centro del sprite
        direction: 'right', 'left', 'up', 'down'
        is_eyes_only: True si solo dibujamos ojos (fantasma comido)
    """
    eye_size = 6 if not is_eyes_only else 8
    pupil_size = 3 if not is_eyes_only else 4
    eye_spacing = 8
    eye_y_offset = -4 if not is_eyes_only else 0
    
    # Posiciones base de los ojos
    left_eye_x = center - eye_spacing // 2 - eye_size // 2
    right_eye_x = center + eye_spacing // 2 - eye_size // 2
    eye_y = center + eye_y_offset
    
    # Dibujar blancos de los ojos
    left_eye_bbox = [left_eye_x, eye_y - eye_size // 2,
                     left_eye_x + eye_size, eye_y + eye_size // 2]
    right_eye_bbox = [right_eye_x, eye_y - eye_size // 2,
                      right_eye_x + eye_size, eye_y + eye_size // 2]
    
    if is_eyes_only:
        draw.ellipse(left_eye_bbox, fill=WHITE, outline=BLACK, width=1)
        draw.ellipse(right_eye_bbox, fill=WHITE, outline=BLACK, width=1)
    else:
        draw.ellipse(left_eye_bbox, fill=WHITE, outline=WHITE)
        draw.ellipse(right_eye_bbox, fill=WHITE, outline=WHITE)
    
    # Calcular posición de las pupilas según dirección
    pupil_offset = 2
    pupil_x_offset = 0
    pupil_y_offset = 0
    
    if direction == 'right':
        pupil_x_offset = pupil_offset
    elif direction == 'left':
        pupil_x_offset = -pupil_offset
    elif direction == 'up':
        pupil_y_offset = -pupil_offset
    elif direction == 'down':
        pupil_y_offset = pupil_offset
    
    # Dibujar pupilas
    left_pupil_x = left_eye_x + eye_size // 2 + pupil_x_offset
    left_pupil_y = eye_y + pupil_y_offset
    right_pupil_x = right_eye_x + eye_size // 2 + pupil_x_offset
    right_pupil_y = eye_y + pupil_y_offset
    
    left_pupil_bbox = [left_pupil_x - pupil_size // 2, left_pupil_y - pupil_size // 2,
                       left_pupil_x + pupil_size // 2, left_pupil_y + pupil_size // 2]
    right_pupil_bbox = [right_pupil_x - pupil_size // 2, right_pupil_y - pupil_size // 2,
                        right_pupil_x + pupil_size // 2, right_pupil_y + pupil_size // 2]
    
    draw.ellipse(left_pupil_bbox, fill=COLORS['eyes_pupil'], outline=COLORS['eyes_pupil'])
    draw.ellipse(right_pupil_bbox, fill=COLORS['eyes_pupil'], outline=COLORS['eyes_pupil'])

def create_ghost_sprite(ghost_name, direction='right', frame=0):
    """
    Crea un sprite de fantasma normal
    
    Args:
        ghost_name: 'blinky', 'pinky', 'inky', 'clyde'
        direction: 'right', 'left', 'up', 'down'
        frame: 0 o 1 (para animación de ondas inferiores)
    
    Returns:
        Image object
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), BACKGROUND)
    draw = ImageDraw.Draw(img)
    
    center = size // 2
    radius = size // 2 - 4
    
    color = COLORS[ghost_name]
    
    # Dibujar cuerpo del fantasma
    create_ghost_body(draw, center, radius, color)
    
    # Dibujar parte inferior ondulada
    create_ghost_wave_bottom(draw, center, radius, color, frame)
    
    # Dibujar ojos
    create_ghost_eyes(draw, center, direction)
    
    return img

def create_vulnerable_ghost_sprite(frame=0, warning=False):
    """
    Crea sprite de fantasma vulnerable (azul)
    
    Args:
        frame: 0 o 1 (animación)
        warning: True para modo advertencia (parpadeando blanco/azul)
    
    Returns:
        Image object
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), BACKGROUND)
    draw = ImageDraw.Draw(img)
    
    center = size // 2
    radius = size // 2 - 4
    
    # Color alterna entre azul y blanco si está en warning
    if warning and frame == 1:
        color = COLORS['warning']
    else:
        color = COLORS['vulnerable']
    
    # Dibujar cuerpo
    create_ghost_body(draw, center, radius, color)
    create_ghost_wave_bottom(draw, center, radius, color, frame)
    
    # Dibujar boca ondulada (característica del modo vulnerable)
    mouth_y = center + 4
    mouth_width = radius
    
    # Dibujar línea ondulada para la boca
    for x in range(-mouth_width, mouth_width, 3):
        y_offset = 2 if (x // 3) % 2 == 0 else 0
        point_x = center + x
        point_y = mouth_y + y_offset
        draw.ellipse([point_x - 1, point_y - 1, point_x + 1, point_y + 1], 
                     fill=WHITE if not warning or frame == 0 else color, 
                     outline=WHITE if not warning or frame == 0 else color)
    
    # Dibujar ojos pequeños (solo puntos blancos)
    eye_spacing = 8
    eye_y = center - 2
    eye_size = 2
    
    left_eye_x = center - eye_spacing // 2
    right_eye_x = center + eye_spacing // 2
    
    draw.ellipse([left_eye_x - eye_size, eye_y - eye_size,
                  left_eye_x + eye_size, eye_y + eye_size], 
                 fill=WHITE, outline=WHITE)
    draw.ellipse([right_eye_x - eye_size, eye_y - eye_size,
                  right_eye_x + eye_size, eye_y + eye_size], 
                 fill=WHITE, outline=WHITE)
    
    return img

def create_ghost_eyes_sprite(direction='right'):
    """
    Crea sprite de solo los ojos (fantasma comido regresando a la base)
    
    Args:
        direction: 'right', 'left', 'up', 'down'
    
    Returns:
        Image object
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), BACKGROUND)
    draw = ImageDraw.Draw(img)
    
    center = size // 2
    
    # Solo dibujar ojos grandes
    create_ghost_eyes(draw, center, direction, is_eyes_only=True)
    
    return img

def create_ghosts_spritesheet():
    """
    Crea el sprite sheet completo de todos los fantasmas
    
    Layout:
    Filas 1-2: Blinky (8 sprites: 4 direcciones × 2 frames)
    Filas 3-4: Pinky (8 sprites: 4 direcciones × 2 frames)
    Filas 5-6: Inky (8 sprites: 4 direcciones × 2 frames)
    Filas 7-8: Clyde (8 sprites: 4 direcciones × 2 frames)
    Fila 9: Vulnerable normal (2 frames) + Vulnerable warning (2 frames) + Eyes (4 direcciones)
    """
    
    # Dimensiones del sprite sheet
    cols = 8
    rows = 9
    
    sheet_width = SPRITE_SIZE * cols
    sheet_height = SPRITE_SIZE * rows
    
    sprite_sheet = Image.new('RGBA', (sheet_width, sheet_height), BACKGROUND)
    
    ghost_names = ['blinky', 'pinky', 'inky', 'clyde']
    directions = ['right', 'left', 'up', 'down']
    
    # Generar sprites de fantasmas normales (primeras 8 filas)
    current_row = 0
    for ghost_name in ghost_names:
        for frame in range(2):
            for col, direction in enumerate(directions):
                sprite = create_ghost_sprite(ghost_name, direction, frame)
                x = col * SPRITE_SIZE
                y = current_row * SPRITE_SIZE
                sprite_sheet.paste(sprite, (x, y))
            current_row += 1
    
    # Fila 9: Estados especiales
    # Vulnerable normal (2 frames)
    for frame in range(2):
        sprite = create_vulnerable_ghost_sprite(frame, warning=False)
        x = frame * SPRITE_SIZE
        y = 8 * SPRITE_SIZE
        sprite_sheet.paste(sprite, (x, y))
    
    # Vulnerable warning (2 frames)
    for frame in range(2):
        sprite = create_vulnerable_ghost_sprite(frame, warning=True)
        x = (2 + frame) * SPRITE_SIZE
        y = 8 * SPRITE_SIZE
        sprite_sheet.paste(sprite, (x, y))
    
    # Eyes only (4 direcciones)
    for col, direction in enumerate(directions):
        sprite = create_ghost_eyes_sprite(direction)
        x = (4 + col) * SPRITE_SIZE
        y = 8 * SPRITE_SIZE
        sprite_sheet.paste(sprite, (x, y))
    
    return sprite_sheet

def create_sprite_map_json():
    """
    Crea un archivo JSON con las coordenadas de cada sprite
    """
    sprite_map = {
        "sprite_size": SPRITE_SIZE,
        "sprites": {
            "blinky": {
                "right": [
                    {"x": 0, "y": 0, "frame": 0},
                    {"x": 0, "y": 32, "frame": 1}
                ],
                "left": [
                    {"x": 32, "y": 0, "frame": 0},
                    {"x": 32, "y": 32, "frame": 1}
                ],
                "up": [
                    {"x": 64, "y": 0, "frame": 0},
                    {"x": 64, "y": 32, "frame": 1}
                ],
                "down": [
                    {"x": 96, "y": 0, "frame": 0},
                    {"x": 96, "y": 32, "frame": 1}
                ]
            },
            "pinky": {
                "right": [
                    {"x": 0, "y": 64, "frame": 0},
                    {"x": 0, "y": 96, "frame": 1}
                ],
                "left": [
                    {"x": 32, "y": 64, "frame": 0},
                    {"x": 32, "y": 96, "frame": 1}
                ],
                "up": [
                    {"x": 64, "y": 64, "frame": 0},
                    {"x": 64, "y": 96, "frame": 1}
                ],
                "down": [
                    {"x": 96, "y": 64, "frame": 0},
                    {"x": 96, "y": 96, "frame": 1}
                ]
            },
            "inky": {
                "right": [
                    {"x": 0, "y": 128, "frame": 0},
                    {"x": 0, "y": 160, "frame": 1}
                ],
                "left": [
                    {"x": 32, "y": 128, "frame": 0},
                    {"x": 32, "y": 160, "frame": 1}
                ],
                "up": [
                    {"x": 64, "y": 128, "frame": 0},
                    {"x": 64, "y": 160, "frame": 1}
                ],
                "down": [
                    {"x": 96, "y": 128, "frame": 0},
                    {"x": 96, "y": 160, "frame": 1}
                ]
            },
            "clyde": {
                "right": [
                    {"x": 0, "y": 192, "frame": 0},
                    {"x": 0, "y": 224, "frame": 1}
                ],
                "left": [
                    {"x": 32, "y": 192, "frame": 0},
                    {"x": 32, "y": 224, "frame": 1}
                ],
                "up": [
                    {"x": 64, "y": 192, "frame": 0},
                    {"x": 64, "y": 224, "frame": 1}
                ],
                "down": [
                    {"x": 96, "y": 192, "frame": 0},
                    {"x": 96, "y": 224, "frame": 1}
                ]
            },
            "vulnerable": {
                "normal": [
                    {"x": 0, "y": 256, "frame": 0},
                    {"x": 32, "y": 256, "frame": 1}
                ],
                "warning": [
                    {"x": 64, "y": 256, "frame": 0},
                    {"x": 96, "y": 256, "frame": 1}
                ]
            },
            "eyes_only": {
                "right": {"x": 128, "y": 256},
                "left": {"x": 160, "y": 256},
                "up": {"x": 192, "y": 256},
                "down": {"x": 224, "y": 256}
            }
        }
    }
    
    import json
    with open('ghosts_sprite_map.json', 'w') as f:
        json.dump(sprite_map, f, indent=2)
    
    print("✅ Archivo JSON de mapeo creado: ghosts_sprite_map.json")

def main():
    print("👻 Generador de Sprites de Fantasmas")
    print("=" * 50)
    
    # Generar sprite sheet de fantasmas
    print("Generando sprite sheet de fantasmas...")
    ghosts_sheet = create_ghosts_spritesheet()
    
    # Guardar sprite sheet
    output_path = 'ghosts_spritesheet.png'
    ghosts_sheet.save(output_path)
    print(f"✅ Sprite sheet guardado: {output_path}")
    
    # Crear mapa de sprites (JSON)
    create_sprite_map_json()
    
    # Información del sprite sheet
    print("\n📊 Información del Sprite Sheet:")
    print(f"   - Tamaño total: {ghosts_sheet.width}x{ghosts_sheet.height} píxeles")
    print(f"   - Tamaño de sprite individual: {SPRITE_SIZE}x{SPRITE_SIZE} píxeles")
    print(f"   - Total de sprites: 40")
    print(f"   - Distribución:")
    print(f"     • Blinky (Rojo): 8 sprites (4 direcciones × 2 frames)")
    print(f"     • Pinky (Rosa): 8 sprites (4 direcciones × 2 frames)")
    print(f"     • Inky (Cian): 8 sprites (4 direcciones × 2 frames)")
    print(f"     • Clyde (Naranja): 8 sprites (4 direcciones × 2 frames)")
    print(f"     • Vulnerable: 2 frames normales + 2 frames warning")
    print(f"     • Ojos solamente: 4 direcciones")
    
    print("\n✨ ¡Generación completada!")
    print(f"   Archivos creados:")
    print(f"   - ghosts_spritesheet.png")
    print(f"   - ghosts_sprite_map.json")

if __name__ == "__main__":
    main()
