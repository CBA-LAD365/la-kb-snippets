const fs = require('fs');
const jose = require('node-jose');
const jws = require('jws');
const VError = require('verror').VError;

function createContextData(id, cb) {
  const payload = {
    contextId: id,
    contextData: {
      customer: {
        id: {
          value: "ACustomerId",
          isAsserted: true
        }
      }
    }
  };

  getSigningKey((err, key) => {
    if (err) return cb(err);
    if (getIsUseJwsConfig()) {
      const signed = jws.sign({
        header: {
          alg: "RS256"
        },
        payload: payload,
        secret: key,
      });
      cb(null, signed);
    } else {
      const compact = {
        format: 'compact'
      };
      jose.JWS.createSign(compact, key)
        .update(JSON.stringify(payload))
        .final()
        .then(jws => cb(null, jws))
        .catch(cb);
    }
  });
}

function createContextDataSpec(id, cb) {
  console.log('Creating context data spec: accountId=%s, contextId=%s', process.env.LA_ACCOUNT_ID, id);
  createContextData(id, (err, cd) => {
    if (err) return cb(err);
    cb(null, {
      contextData: cd,
      contextDataCertificate: getServerCert(),
    });
  });
}

function getSigningKey(cb) {
  const keyPem = getSigningKeyPem();
  if (getIsUseJwsConfig()) {
    cb(null, keyPem);
  } else {
    jose.JWK.asKey(keyPem, 'pem')
      .then(key => cb(null, key))
      .catch(cb);
  }
}

function getIsUseJwsConfig() {
  return !process.env.BOT_USE_JWS_LIB || process.env.BOT_USE_JWS_LIB !== 'false';
}

function getSigningKeyPem() {
  // If sgnKeyPem is a file we can read from, use the contents otherwise
  // use it as it is.
  const sgnKeyPem = getSigningKeyPemConfig();
  try {
    return fs.readFileSync(sgnKeyPem).toString();
  } catch (e) {
    return sgnKeyPem.replace(/\\n/g, '\n');
  }
}

function getSigningKeyPemConfig() {
  return (process.env.BOT_CTX_SGN_KEY_PEM === undefined) ? './keys/jwt/prv.pem' : process.env.BOT_CTX_SGN_KEY_PEM;
}

function getServerCert() {
  const serverCert = getServerCertPemConfig();
  if (serverCert === '') return;
  if (serverCert === 'accept') return 'accept';
  try {
    return fs.readFileSync(serverCert);
  } catch (error) {
    return Buffer.from(serverCert.replace(/\\n/g, '\n'));
  }
}

function getServerCertPemConfig() {
  return (process.env.BOT_CTX_SRV_CERT === undefined) ? './keys/srv/cert.pem' : process.env.BOT_CTX_SRV_CERT;
}

exports.createContextDataSpec = createContextDataSpec;