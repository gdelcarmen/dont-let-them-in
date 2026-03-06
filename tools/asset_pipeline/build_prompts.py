import json
from pathlib import Path


ROOT = Path(__file__).resolve().parent
MANIFEST = ROOT / "manifest.json"


def compose_prompt(item, style_prefix, alien_modifier, house_modifier):
    prompt_type = item["prompt_type"]
    desc = item["description"]
    is_alien = item["category"] == "enemy" or "alien" in desc.lower() or "sci-fi" in desc.lower()
    modifier = alien_modifier if is_alien else house_modifier

    if prompt_type == "character":
        suffix = "full body, front-facing, T-pose, clean white background, isolated character, sharp edges, no shadows on background"
    elif prompt_type == "prop":
        suffix = "centered, clean white background, isolated object, product shot style, sharp edges, no shadows on background"
    elif prompt_type == "environment":
        suffix = "top-down view, orthographic perspective, clean edges, tileable, flat lighting"
    elif prompt_type == "texture":
        return f"Stylized cartoon game texture, {desc}, seamless tileable texture, flat, no perspective distortion"
    else:
        return f"{desc}, game UI element, clean vector style, transparent background, icon design, bold outlines, warm and cool contrast"

    return f"{style_prefix} {modifier} {desc}, {suffix}"


def main():
    data = json.loads(MANIFEST.read_text())
    prompts = {}
    for item in data["assets"]:
        prompts[item["name"]] = compose_prompt(
            item,
            data["style_prefix"],
            data["alien_modifier"],
            data["house_modifier"],
        )
    print(json.dumps(prompts, indent=2))


if __name__ == "__main__":
    main()
