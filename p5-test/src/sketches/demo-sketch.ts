import type p5 from "p5"


let size = 0.5;

// setInterval(() => {
//     size = 10 + (1 + Math.sin(Date.now() / 1000)) / 2 * 60;
// })

const demoSketch = (p: p5) => {
	let x = 0
	let y = 0
	let speedX = 2
	let speedY = 2

	p.setup = () => {
		const canvas = p.createCanvas(p.windowWidth, p.windowHeight)
		canvas.parent('app')
		p.background(0)
	}

	p.draw = () => {

		p.background(0)
		
		p.circle(0.5 * p.windowWidth, 0.5 * p.windowHeight, size).fill(255, 150, 20)
	}

	p.windowResized = () => {
		p.resizeCanvas(p.windowWidth, p.windowHeight)
	}

    

	p.mousePressed = () => {
		
	}
}

export default demoSketch