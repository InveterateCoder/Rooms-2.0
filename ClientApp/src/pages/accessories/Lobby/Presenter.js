import React, { useContext } from "react";
import { Context } from "../../../data/Context";
import { Link } from "react-router-dom";
import Flag from "react-flags";
import LocalizedStrings from "react-localization";
const text = new LocalizedStrings({
    en: {
        online: "Online",
        sorry: "SORRY",
        norecord: "No records yet"
    },
    ru: {
        online: "В сети",
        sorry: "ЖАЛЬ",
        norecord: "Пока нет записей"
    }
})

export function Presenter(props) {
    const context = useContext(Context);
    const getContent = item => {
        let textColor = context.theme === "dark" ? "light" : "dark";
        return <><h4 className="card-title">{item.locked && <img src="/img/lock.png" width="32" alt="lock imag" />}
            <span><Flag name={item.flag} format="png" pngSize={32} alt={"flat" + item.flag} basePath="/img" /></span><span className="name">{item.name}</span></h4>
            <strong className={`text-${textColor}`}>
                {text.online}: <span className={`badge badge-${textColor} text-${context.theme}`}>{item.online}</span></strong>
            <p className={`card-text text-${textColor}`}>{item.description}</p></>
    }
    const newWindow = addr => {
        let width = window.innerWidth < 1150 ? window.innerWidth : 1150;
        let height = window.innerHeight < 700 ? window.innerHeight : 700;
        let right = window.screen.width - window.outerWidth - window.screenLeft;
        let bottom = window.screen.height - window.outerHeight - window.screenTop;
        let xdiff = window.screenLeft - right - 24;
        let ydiff = window.screenTop - bottom;
        let left = (window.screen.width - width + xdiff) / 2;
        let top = (window.screen.height - height + ydiff) / 2;
        window.open(addr, "", `width=${width},height=${height},left=${left},top=${top}`);
    }
    text.setLanguage(context.lang);
    return <div id="content">
        {
            !props.list || props.list.length < 1
                ? <div id="no_records">
                    <img src="/img/search.png" width="128" alt="Search" />
                    <span className="display-4 text-danger">{text.sorry}</span>
                    <div className="display-4">{text.norecord}</div>
                </div>
                : props.list.map(item =>
                    <div key={item.slug} className="card">
                        {
                            context.openin === "nw"
                                ? <a href={"/room/" + item.slug}
                                    onClick={ev => { ev.preventDefault(); newWindow("/room/" + item.slug) }}
                                    className="card-body">
                                    {getContent(item)}
                                </a>
                                : <Link to={"/room/" + item.slug}
                                    target={context.openin === "nt" ? "_blank" : "_self"}
                                    className="card-body">
                                    {getContent(item)}
                                </Link>
                        }
                    </div>)
        }
    </div>
}