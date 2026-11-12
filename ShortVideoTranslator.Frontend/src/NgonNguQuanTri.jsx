import { createContext, useContext } from 'react';
import { chuQuanTri } from "./tuDienQuanTri";
export const NgonNguQuanTri = createContext('vi');
export const useChuQuanTri = () => {
  const language = useContext(NgonNguQuanTri);
  return text => chuQuanTri(language, text);
};
