import os
from PIL import Image, ImageDraw

def draw_pacman(size):
    """
    Draws a Pac-Man icon at the specified size.

    Args:
        size (int): Size of the icon (width and height)

    Returns:
        Image: PIL Image object with Pac-Man drawn
    """
    # Create image with transparent background
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Calculate dimensions
    margin = size // 10  # 10% margin
    diameter = size - (2 * margin)

    # Pac-Man body (yellow circle with mouth)
    body_bbox = [margin, margin, margin + diameter, margin + diameter]

    # Draw yellow circle (Pac-Man body)
    # Mouth angle: 30 degrees open
    draw.pieslice(body_bbox, 30, 330, fill='#FFFF00', outline='#FFD700', width=max(1, size // 50))

    # Draw eye
    eye_size = max(2, size // 12)
    eye_x = margin + (diameter // 2) + (diameter // 6)
    eye_y = margin + (diameter // 4)
    eye_bbox = [eye_x - eye_size, eye_y - eye_size, eye_x + eye_size, eye_y + eye_size]
    draw.ellipse(eye_bbox, fill='#000000')

    return img

def generate_all_icons():
    """
    Generates all required icons for the application.
    Creates icons from scratch without needing external images.
    """
    print("Pacman Recreation - Icon Generator")
    print("=" * 50)
    print("Generating icons from scratch...\n")

    # Define paths
    script_dir = os.path.dirname(os.path.abspath(__file__))
    base_dir = os.path.dirname(os.path.dirname(script_dir))
    assets_dir = os.path.join(base_dir, "src", "PacmanGame", "Assets")
    flatpak_icons_dir = os.path.join(base_dir, "flatpak", "icons")

    # Ensure directories exist
    os.makedirs(assets_dir, exist_ok=True)

    print("Step 1: Generating Windows ICO...")
    print("-" * 50)

    # Generate Windows ICO (multi-resolution)
    ico_sizes = [16, 32, 48, 256]
    ico_images = []

    for size in ico_sizes:
        icon = draw_pacman(size)
        ico_images.append(icon)
        print(f"  Generated {size}x{size} icon layer")

    ico_path = os.path.join(assets_dir, "icon.ico")
    ico_images[0].save(
        ico_path,
        format='ICO',
        sizes=[(img.size[0], img.size[1]) for img in ico_images],
        append_images=ico_images[1:]
    )
    print(f"Saved: {ico_path}\n")

    print("Step 2: Generating Fallback PNG...")
    print("-" * 50)

    # Generate fallback PNG (256x256)
    png_icon = draw_pacman(256)
    png_path = os.path.join(assets_dir, "icon.png")
    png_icon.save(png_path, format='PNG', optimize=True)
    print(f"Saved: {png_path}\n")

    print("Step 3: Generating Flatpak Icons...")
    print("-" * 50)

    # Generate Flatpak hicolor icons
    flatpak_sizes = [64, 128, 256, 512]
    app_id = "com.codewithbotina.PacmanRecreation"

    for size in flatpak_sizes:
        size_dir = os.path.join(flatpak_icons_dir, f"{size}x{size}")
        os.makedirs(size_dir, exist_ok=True)

        icon_path = os.path.join(size_dir, f"{app_id}.png")
        flatpak_icon = draw_pacman(size)
        flatpak_icon.save(icon_path, format='PNG', optimize=True)
        print(f"  Saved: {icon_path}")

    print("\n" + "=" * 50)
    print("Icon generation complete!")
    print("=" * 50)
    print("\nGenerated files:")
    print(f"  Windows ICO:  {ico_path}")
    print(f"  Fallback PNG: {png_path}")
    print(f"  Flatpak:      {flatpak_icons_dir}/{{64,128,256,512}}x{{64,128,256,512}}/{app_id}.png")
    print("\nYou can now use these icons in your application.")

if __name__ == "__main__":
    try:
        generate_all_icons()
    except Exception as e:
        print(f"\nError: {e}")
        print("Make sure you have Pillow installed: pip install Pillow")
        exit(1)
