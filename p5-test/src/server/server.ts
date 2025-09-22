import createOscQueryServer from "../osc-query/oscquery-server";

const port = 45124;


const server = createOscQueryServer();

server.listen(port, () => {
    console.log(`Server is running on port ${port}`);
});