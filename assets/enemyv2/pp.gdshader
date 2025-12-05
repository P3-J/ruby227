shader_type spatial;

uniform sampler2D albedo_tex : source_color;
uniform float scroll_speed = 1.0;
uniform bool scrolling_enabled = true;

void fragment() {
    vec2 uv_mod = UV;

    if (scrolling_enabled) {
        uv_mod.y += TIME * scroll_speed;
    }

    ALBEDO = texture(albedo_tex, uv_mod).rgb;
}
