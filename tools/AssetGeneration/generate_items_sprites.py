#!/usr/bin/env python3
"""
Generador de Sprite Sheet de Items - Estilo 8-bit Clásico
Genera sprites de puntos, power pellets y frutas para Pac-Man
"""

from PIL import Image, ImageDraw
import math

# Configuración
SPRITE_SIZE = 32  # Tamaño de cada sprite individual

# Colores (RGB)
COLORS = {
    'dot': (255, 184, 174),          # Rosa claro para puntos pequeños
    'power_pellet': (255, 255, 255),  # Blanco para power pellets
    'cherry_red': (255, 0, 0),        # Rojo cereza
    'cherry_green': (0, 255, 0),      # Verde tallo
    'strawberry_red': (255, 0, 0),    # Rojo fresa
    'strawberry_green': (0, 200, 0),  # Verde hojas
    'strawberry_seed': (255, 200, 100), # Semillas
    'orange_orange': (255, 165, 0),   # Naranja
    'orange_green': (0, 200, 0),      # Verde hoja
    'apple_red': (220, 20, 60),       # Rojo manzana
    'apple_green': (0, 200, 0),       # Verde hoja/tallo
    'apple_highlight': (255, 100, 100), # Brillo
    'melon_green': (50, 205, 50),     # Verde melón
    'melon_dark': (34, 139, 34),      # Verde oscuro
    'melon_light': (144, 238, 144)    # Verde claro
}

BACKGROUND = (0, 0, 0, 0)  # Transparente
WHITE = (255, 255, 255)
BLACK = (0, 0, 0)

def create_small_dot():
    """
    Crea un punto pequeño (dot)
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), BACKGROUND)
    draw = ImageDraw.Draw(img)
    
    center = size // 2
    dot_radius = 3
    
    bbox = [center - dot_radius, center - dot_radius,
            center + dot_radius, center + dot_radius]
    
    draw.ellipse(bbox, fill=COLORS['dot'], outline=COLORS['dot'])
    
    return img

def create_power_pellet(frame=0):
    """
    Crea un power pellet (punto grande) con animación de parpadeo
    
    Args:
        frame: 0 o 1 (para animación de parpadeo)
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), BACKGROUND)
    draw = ImageDraw.Draw(img)
    
    center = size // 2
    
    # Alternar tamaño para efecto de parpadeo
    if frame == 0:
        pellet_radius = 7
    else:
        pellet_radius = 6
    
    bbox = [center - pellet_radius, center - pellet_radius,
            center + pellet_radius, center + pellet_radius]
    
    draw.ellipse(bbox, fill=COLORS['power_pellet'], outline=COLORS['power_pellet'])
    
    return img

def create_cherry():
    """
    Crea sprite de cereza
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), BACKGROUND)
    draw = ImageDraw.Draw(img)
    
    center = size // 2
    cherry_radius = 6
    
    # Tallo (línea curva)
    stem_points = [
        (center, center - 8),
        (center - 2, center - 6),
        (center - 3, center - 4)
    ]
    draw.line(stem_points, fill=COLORS['cherry_green'], width=2)
    
    stem_points_2 = [
        (center, center - 8),
        (center + 2, center - 6),
        (center + 4, center - 3)
    ]
    draw.line(stem_points_2, fill=COLORS['cherry_green'], width=2)
    
    # Hoja pequeña
    leaf_points = [
        (center - 1, center - 8),
        (center - 4, center - 10),
        (center - 1, center - 9)
    ]
    draw.polygon(leaf_points, fill=COLORS['cherry_green'])
    
    # Cereza izquierda
    cherry_left_x = center - 3
    cherry_left_y = center - 1
    bbox_left = [cherry_left_x - cherry_radius, cherry_left_y - cherry_radius,
                 cherry_left_x + cherry_radius, cherry_left_y + cherry_radius]
    draw.ellipse(bbox_left, fill=COLORS['cherry_red'], outline=COLORS['cherry_red'])
    
    # Brillo en cereza izquierda
    highlight_bbox_left = [cherry_left_x - 2, cherry_left_y - 3,
                           cherry_left_x + 1, cherry_left_y]
    draw.ellipse(highlight_bbox_left, fill=(255, 100, 100))
    
    # Cereza derecha
    cherry_right_x = center + 4
    cherry_right_y = center + 1
    bbox_right = [cherry_right_x - cherry_radius, cherry_right_y - cherry_radius,
                  cherry_right_x + cherry_radius, cherry_right_y + cherry_radius]
    draw.ellipse(bbox_right, fill=COLORS['cherry_red'], outline=COLORS['cherry_red'])
    
    # Brillo en cereza derecha
    highlight_bbox_right = [cherry_right_x - 2, cherry_right_y - 3,
                            cherry_right_x + 1, cherry_right_y]
    draw.ellipse(highlight_bbox_right, fill=(255, 100, 100))
    
    return img

def create_strawberry():
    """
    Crea sprite de fresa
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), BACKGROUND)
    draw = ImageDraw.Draw(img)
    
    center = size // 2
    
    # Cuerpo de la fresa (forma ovalada)
    body_width = 10
    body_height = 12
    
    body_points = [
        (center, center - body_height // 2),  # Top
        (center + body_width // 2, center),    # Right
        (center, center + body_height // 2),   # Bottom
        (center - body_width // 2, center)     # Left
    ]
    
    # Dibujar cuerpo usando polígono
    bbox = [center - body_width // 2, center - body_height // 2,
            center + body_width // 2, center + body_height // 2]
    draw.ellipse(bbox, fill=COLORS['strawberry_red'], outline=COLORS['strawberry_red'])
    
    # Hojas verdes arriba (3 hojas)
    leaf_y = center - body_height // 2 - 2
    
    # Hoja central
    leaf_center = [
        (center, leaf_y - 4),
        (center - 2, leaf_y),
        (center + 2, leaf_y)
    ]
    draw.polygon(leaf_center, fill=COLORS['strawberry_green'])
    
    # Hoja izquierda
    leaf_left = [
        (center - 3, leaf_y - 3),
        (center - 6, leaf_y - 1),
        (center - 3, leaf_y)
    ]
    draw.polygon(leaf_left, fill=COLORS['strawberry_green'])
    
    # Hoja derecha
    leaf_right = [
        (center + 3, leaf_y - 3),
        (center + 6, leaf_y - 1),
        (center + 3, leaf_y)
    ]
    draw.polygon(leaf_right, fill=COLORS['strawberry_green'])
    
    # Semillas (puntos amarillos pequeños)
    seeds = [
        (center - 3, center - 2),
        (center + 3, center - 2),
        (center - 4, center + 2),
        (center, center + 3),
        (center + 4, center + 2)
    ]
    
    for seed_x, seed_y in seeds:
        draw.ellipse([seed_x - 1, seed_y - 1, seed_x + 1, seed_y + 1],
                     fill=COLORS['strawberry_seed'])
    
    return img

def create_orange():
    """
    Crea sprite de naranja
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), BACKGROUND)
    draw = ImageDraw.Draw(img)
    
    center = size // 2
    orange_radius = 8
    
    # Cuerpo de la naranja
    bbox = [center - orange_radius, center - orange_radius,
            center + orange_radius, center + orange_radius]
    draw.ellipse(bbox, fill=COLORS['orange_orange'], outline=COLORS['orange_orange'])
    
    # Brillo
    highlight_bbox = [center - 4, center - 5,
                      center - 1, center - 2]
    draw.ellipse(highlight_bbox, fill=(255, 200, 100))
    
    # Tallo pequeño arriba
    stem_bbox = [center - 1, center - orange_radius - 3,
                 center + 1, center - orange_radius]
    draw.rectangle(stem_bbox, fill=(139, 69, 19))  # Marrón
    
    # Hoja
    leaf_points = [
        (center + 2, center - orange_radius - 2),
        (center + 5, center - orange_radius - 4),
        (center + 3, center - orange_radius - 1)
    ]
    draw.polygon(leaf_points, fill=COLORS['orange_green'])
    
    # Textura de naranja (pequeños puntos)
    import random
    random.seed(42)  # Para consistencia
    for _ in range(15):
        angle = random.uniform(0, 2 * math.pi)
        distance = random.uniform(3, orange_radius - 2)
        dot_x = int(center + distance * math.cos(angle))
        dot_y = int(center + distance * math.sin(angle))
        draw.point((dot_x, dot_y), fill=(200, 100, 0))
    
    return img

def create_apple():
    """
    Crea sprite de manzana
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), BACKGROUND)
    draw = ImageDraw.Draw(img)
    
    center = size // 2
    apple_radius = 8
    
    # Cuerpo de la manzana (círculo con hendidura arriba)
    bbox = [center - apple_radius, center - apple_radius + 2,
            center + apple_radius, center + apple_radius]
    draw.ellipse(bbox, fill=COLORS['apple_red'], outline=COLORS['apple_red'])
    
    # Parte superior (dos semi-círculos)
    top_left_bbox = [center - apple_radius, center - apple_radius,
                     center, center]
    draw.ellipse(top_left_bbox, fill=COLORS['apple_red'], outline=COLORS['apple_red'])
    
    top_right_bbox = [center, center - apple_radius,
                      center + apple_radius, center]
    draw.ellipse(top_right_bbox, fill=COLORS['apple_red'], outline=COLORS['apple_red'])
    
    # Hendidura en el centro superior
    indent_bbox = [center - 2, center - apple_radius,
                   center + 2, center - apple_radius + 3]
    draw.ellipse(indent_bbox, fill=(150, 0, 0))
    
    # Tallo
    stem_bbox = [center - 1, center - apple_radius - 3,
                 center + 1, center - apple_radius + 1]
    draw.rectangle(stem_bbox, fill=(101, 67, 33))  # Marrón
    
    # Hoja
    leaf_points = [
        (center + 2, center - apple_radius - 1),
        (center + 6, center - apple_radius - 3),
        (center + 3, center - apple_radius)
    ]
    draw.polygon(leaf_points, fill=COLORS['apple_green'])
    
    # Brillo
    highlight_bbox = [center - 4, center - 4,
                      center - 1, center - 1]
    draw.ellipse(highlight_bbox, fill=COLORS['apple_highlight'])
    
    return img

def create_melon():
    """
    Crea sprite de melón
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), BACKGROUND)
    draw = ImageDraw.Draw(img)
    
    center = size // 2
    melon_width = 14
    melon_height = 10
    
    # Cuerpo del melón (elipse)
    bbox = [center - melon_width // 2, center - melon_height // 2,
            center + melon_width // 2, center + melon_height // 2]
    draw.ellipse(bbox, fill=COLORS['melon_green'], outline=COLORS['melon_green'])
    
    # Rayas verticales oscuras
    num_stripes = 5
    for i in range(num_stripes):
        x = center - melon_width // 2 + (i * (melon_width // (num_stripes - 1)))
        
        # Calcular altura de la raya basada en la forma elíptica
        # Usar ecuación de elipse para determinar y
        if abs(x - center) <= melon_width // 2:
            relative_x = (x - center) / (melon_width / 2)
            half_height = int(melon_height / 2 * math.sqrt(1 - relative_x ** 2))
            
            stripe_top = center - half_height
            stripe_bottom = center + half_height
            
            draw.line([(x, stripe_top), (x, stripe_bottom)],
                     fill=COLORS['melon_dark'], width=2)
    
    # Brillo
    highlight_bbox = [center - 5, center - 3,
                      center - 2, center]
    draw.ellipse(highlight_bbox, fill=COLORS['melon_light'])
    
    return img

def create_items_spritesheet():
    """
    Crea el sprite sheet completo de items
    
    Layout (1 fila):
    [Dot][Power1][Power2][Cherry][Strawberry][Orange][Apple][Melon]
    """
    
    # Dimensiones del sprite sheet
    cols = 8
    rows = 1
    
    sheet_width = SPRITE_SIZE * cols
    sheet_height = SPRITE_SIZE * rows
    
    sprite_sheet = Image.new('RGBA', (sheet_width, sheet_height), BACKGROUND)
    
    # Lista de sprites en orden
    sprites = [
        create_small_dot(),           # 0: Dot
        create_power_pellet(0),       # 1: Power pellet frame 0
        create_power_pellet(1),       # 2: Power pellet frame 1
        create_cherry(),              # 3: Cherry
        create_strawberry(),          # 4: Strawberry
        create_orange(),              # 5: Orange
        create_apple(),               # 6: Apple
        create_melon()                # 7: Melon
    ]
    
    # Pegar sprites en el sheet
    for col, sprite in enumerate(sprites):
        x = col * SPRITE_SIZE
        y = 0
        sprite_sheet.paste(sprite, (x, y))
    
    return sprite_sheet

def create_sprite_map_json():
    """
    Crea un archivo JSON con las coordenadas de cada sprite
    """
    sprite_map = {
        "sprite_size": SPRITE_SIZE,
        "sprites": {
            "dot": {
                "x": 0,
                "y": 0,
                "points": 10
            },
            "power_pellet": {
                "frames": [
                    {"x": 32, "y": 0, "frame": 0},
                    {"x": 64, "y": 0, "frame": 1}
                ],
                "points": 50
            },
            "fruits": {
                "cherry": {
                    "x": 96,
                    "y": 0,
                    "points": 100,
                    "effect": "extra_life"
                },
                "strawberry": {
                    "x": 128,
                    "y": 0,
                    "points": 300,
                    "effect": "none"
                },
                "orange": {
                    "x": 160,
                    "y": 0,
                    "points": 500,
                    "effect": "slow_ghosts"
                },
                "apple": {
                    "x": 192,
                    "y": 0,
                    "points": 700,
                    "effect": "more_power_pellets"
                },
                "melon": {
                    "x": 224,
                    "y": 0,
                    "points": 1000,
                    "effect": "invincibility"
                }
            }
        }
    }
    
    import json
    with open('items_sprite_map.json', 'w') as f:
        json.dump(sprite_map, f, indent=2)
    
    print("✅ Archivo JSON de mapeo creado: items_sprite_map.json")

def main():
    print("🍬 Generador de Sprites de Items")
    print("=" * 50)
    
    # Generar sprite sheet de items
    print("Generando sprite sheet de items...")
    items_sheet = create_items_spritesheet()
    
    # Guardar sprite sheet
    output_path = 'items_spritesheet.png'
    items_sheet.save(output_path)
    print(f"✅ Sprite sheet guardado: {output_path}")
    
    # Crear mapa de sprites (JSON)
    create_sprite_map_json()
    
    # Información del sprite sheet
    print("\n📊 Información del Sprite Sheet:")
    print(f"   - Tamaño total: {items_sheet.width}x{items_sheet.height} píxeles")
    print(f"   - Tamaño de sprite individual: {SPRITE_SIZE}x{SPRITE_SIZE} píxeles")
    print(f"   - Total de sprites: 8")
    print(f"   - Distribución:")
    print(f"     • Punto pequeño (dot): 1 sprite")
    print(f"     • Power pellet: 2 frames (animación de parpadeo)")
    print(f"     • Cereza: 1 sprite (vida extra)")
    print(f"     • Fresa: 1 sprite")
    print(f"     • Naranja: 1 sprite (ralentizar fantasmas)")
    print(f"     • Manzana: 1 sprite (más power pellets)")
    print(f"     • Melón: 1 sprite (invencibilidad)")
    
    print("\n💡 Efectos sugeridos por fruta:")
    print("   - 🍒 Cereza: Vida extra")
    print("   - 🍓 Fresa: Solo puntos")
    print("   - 🍊 Naranja: Ralentiza fantasmas")
    print("   - 🍎 Manzana: Más power pellets en el mapa")
    print("   - 🍉 Melón: Invencibilidad temporal")
    
    print("\n✨ ¡Generación completada!")
    print(f"   Archivos creados:")
    print(f"   - items_spritesheet.png")
    print(f"   - items_sprite_map.json")

if __name__ == "__main__":
    main()
