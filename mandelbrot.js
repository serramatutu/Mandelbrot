'use strict';

const ITERACOES = 500;
const WIDTH = 1920; //1920;
const HEIGHT = 1080; //1080;

// Coordenadas em 0, 0 resultam na imagem padrão do conjunto

var ZOOM = 1,
    COORD_X = 0,
    COORD_Y = 0;

// Julia Island
// ZOOM = 59979000000;
// COORD_X = -.743643887029151;
// COORD_Y = -.131825904205330;



function MandelbrotPoint(x, y) {
    this.startX = x;
    this.startY = y;
    this.x = x;
    this.y = y;

    this.escaped = false;
}

MandelbrotPoint.prototype.tick = function() {
    if (this.escaped)
        return false;

    var aux = this.x*this.x - this.y*this.y + this.startX;
    this.y = 2*this.x*this.y + this.startY;
    this.x = aux;

    if (this.x*this.x + this.y*this.y >= 4) { // Se escapou nesta chamada
        this.escaped = true;
        return true;
    }

    return false;
}

window.onload = function() {
    var canvas = document.createElement('canvas');
    canvas.width = WIDTH;
    canvas.height = HEIGHT;
    document.body.appendChild(canvas);

    var ctx = canvas.getContext('2d');

    var startTime = new Date();
    console.log('Inicializando pontos');
    // Inicia o vetor de pontos
    var points = new Array(WIDTH);
    for (var i=0; i<WIDTH; i++) {
        points[i] = new Array(HEIGHT);
        for (var j=0; j<HEIGHT; j++) {
            // points[i][j] = new MandelbrotPoint((i * 3 / ZOOM) / WIDTH - 2,
            //                                    (j * 2 / ZOOM) / HEIGHT - 1); // Define o ponto de início e fim
            points[i][j] = new MandelbrotPoint((i * 3 / ZOOM) / WIDTH - 2/ZOOM + COORD_X,
                                               (j * 2 / ZOOM) / HEIGHT - 1/ZOOM + COORD_Y); // Define o ponto de início e fim
        }


    }

    //  Variáveis de cor
    //  Start   End       Delta
    var sr = 0, er = 0, deltaRed = er - sr,
        sb = 0, eb = 0, deltaBlue = eb - sb,
        sg = 0, eg = 255, deltaGreen = eg - sg;

    ctx.fillStyle = '#' + ((sr << 16) | (sb << 8) | sg).toString(16);

    ctx.fillRect(0, 0, WIDTH, HEIGHT);
    // Itera sobre cada ponto
    var porcentagemIteracao, ri, gi, bi;

    console.log('Iniciando calculo...');
    for (var iteracao = 0; iteracao < ITERACOES; iteracao++) {
        var porcentagemIteracao = iteracao/ITERACOES;

        ri = (sr + deltaRed * porcentagemIteracao) << 16;
        gi = (sg + deltaBlue * porcentagemIteracao);
        bi = (sb + deltaGreen * porcentagemIteracao) << 8;

        var color = (ri | gi | bi).toString(16);
        while (color.length < 6)
            color = "0"+color;
        ctx.fillStyle = '#' + color;

        for (var i=0; i<WIDTH; i++)
            for (var j=0; j<HEIGHT; j++)
                if (points[i][j].tick()) // Se escapou nesta iteração
                    ctx.fillRect(i, j, 1, 1);
    }
    var endTime = new Date();
    console.log('Pronto! Tempo total: ' + ((endTime - startTime) / 1000) + 's');
}
