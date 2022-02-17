- Quick and dirty implementation of Gemini protocol server.
- Files are served from `public` directory, next to binary.
- Support `text/gemini` based on `.gmi` extension (client can request `/file` or `/file.gmi`).
- Other file are served directly, with MIME type based on extension.
- Uses `certificate.pfx` for server certificate.
- Hardcoded on purpose

### generate certificate
```
openssl req -x509 -newkey rsa:4096 -keyout key.pem -out cert.pem -days 10000 -nodes
openssl pkcs12 -export -out certificate.pfx -inkey key.pem -in cert.pem
```