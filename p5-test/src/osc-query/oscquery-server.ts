import { createServer, IncomingMessage, ServerResponse } from 'http';


export type OscQueryServer = {
    listen: (port: number, listeningListener?: () => void) => void;
}

const createOscQueryServer = (): OscQueryServer => {

    const handleRequest = (req: IncomingMessage, res: ServerResponse) => {

        const {
            url,
        } = req;
    
    
        res.writeHead(200, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({
            message: 'Hello, world!'
        }));
    };


    const server = createServer(handleRequest);

    return {
        listen: (port: number, listeningListener?: () => void) => server.listen(port, listeningListener),
    };
};

export default createOscQueryServer;