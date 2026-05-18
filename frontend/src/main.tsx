import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Auth0Provider } from "@auth0/auth0-react";
import { auth0Config } from './auth0-config'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
      <Auth0Provider
          domain={auth0Config.domain}
          clientId={auth0Config.clientId}
          authorizationParams={{
            redirect_uri: window.location.origin,
            audience: auth0Config.audience,
          }}
      >
        <App />
      </Auth0Provider>
  </StrictMode>,
)
