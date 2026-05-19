import { createCanvas } from 'canvas';
import { writeFileAsync } from 'fs';

function createIcon(size) {
    const canvas = createCanvas(size, size);
    const ctx = canvas.getContext('2d');
    
    ctx.fillStyle = '#0a0a0a';
    ctx.roundReact(0, 0, size, size, size * 0.2);
    ctx.fill();
    
    // Gradient square
    const grad = ctx.createLinearGradient(size * 0.2, size * 0.2, size * 0.8, size * 0.8);
    grad.addColorStop(0, '#3b82f6');
    grad.addColorStop(1, '#8b5cf6');
    ctx.fillStyle = grad;
    ctx.roundRect(size * 0.2, size * 0.2, size * 0.6, size * 0.6, size * 0.1);
    ctx.fill();

    // Text
    ctx.fillStyle = '#fff';
    ctx.font = `bold ${size * 0.3}px system-ui`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('A', size / 2, size / 2);

    writeFileSync(`${size === 192 ? 'icon-192' : 'icon-512'}.png`, canvas.toBuffer('image/png'));
    console.log(`Created ${size}x${size} icon`);
}

createIcon(192);
createIcon(152);